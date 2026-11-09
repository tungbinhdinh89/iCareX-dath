using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Infrastructure.Services.Implementations;
using iCareX.Shared.Contracts.Identity;
using iCareX.Shared.Contracts.Vehicles;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed partial class DatabaseTests
{
    private static void ConfigureEmail(IServiceCollection services, CapturedEmail mail)
    {
        services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
        services.RemoveAll<IAccountEmailSender>();
        services.AddSingleton<IAccountEmailSender>(mail);
        services.Configure<AccountEmailOptions>(x => x.PublicBaseUrl = "https://icarex.example.test");
    }

    // Tests capture delivered messages in memory; production always uses configured SMTP.
    private sealed class CapturedEmail : IAccountEmailSender
    {
        private readonly ConcurrentDictionary<string, string> messages = new();
        public void EnsureConfigured() { }
        public Task SendAsync(string recipient, string subject, string text, CancellationToken ct)
        { messages[recipient] = text; return Task.CompletedTask; }
        public Uri Link(string address) => new(messages[address].Split('\n').Single(x => x.StartsWith("https://", StringComparison.Ordinal)));
        public string Token(string address) => QueryHelpers.ParseQuery(Link(address).Fragment.TrimStart('#'))["token"].ToString();
        public bool Contains(string address) => messages.ContainsKey(address);
    }

    private WebApplicationFactory<Program> SecurityHost(CapturedEmail mail) => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseEnvironment("Development");
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:iCareX"] = postgres.GetConnectionString() }));
        b.ConfigureServices(s => ConfigureEmail(s, mail));
    });

    private static async Task<HttpClient> SecurityClient(WebApplicationFactory<Program> host)
    {
        var client = host.CreateClient();
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        return client;
    }

    [Fact]
    public async Task VerificationAndPasswordRecoveryAreSingleUseAndRevokeSessions()
    {
        var mail = new CapturedEmail();
        using var host = SecurityHost(mail);
        using var client = await SecurityClient(host);
        var email = $"{Guid.NewGuid():N}@example.test";
        const string original = "OriginalPassword@123";
        const string replacement = "ReplacementPassword@123";
        const string changed = "ChangedPassword@123";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new { name = "Security user", email, password = original, phone = "" });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var account = (await register.Content.ReadFromJsonAsync<AccountResponse>())!;
        Assert.Equal("PendingVerification", account.AccountStatus);
        Assert.False(account.EmailVerified);
        var verification = mail.Token(email);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = original })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/vehicles")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { email, token = "invalid" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { email, token = verification })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { email, token = verification })).StatusCode);
        Assert.True((await client.GetFromJsonAsync<AccountResponse>("/api/v1/auth/me"))!.EmailVerified);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/vehicles")).StatusCode);
        var unknown = $"{Guid.NewGuid():N}@example.test";
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = unknown })).StatusCode);
        Assert.False(mail.Contains(unknown));
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email })).StatusCode);
        var reset = mail.Token(email);
        await using (var db = Context())
            Assert.DoesNotContain(reset, await db.UserTokens.Where(x => x.UserId == account.Id).Select(x => x.Value).ToArrayAsync());
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token = reset, newPassword = "weak" })).StatusCode);
        // Two requests race for the same Identity token; exactly one transaction may consume it.
        var resets = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token = reset, newPassword = replacement }),
            client.PostAsJsonAsync("/api/v1/auth/reset-password", new { email, token = reset, newPassword = replacement }));
        Assert.Single(resets, x => x.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(resets, x => x.StatusCode == HttpStatusCode.BadRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = original })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = replacement })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = original, newPassword = changed })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = replacement, newPassword = changed })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = changed })).StatusCode);
    }

    [Fact]
    public async Task InvitationsRequireEmailMatchExplicitAcceptAndAreSingleUse()
    {
        var mail = new CapturedEmail();
        using var host = SecurityHost(mail);
        async Task<(HttpClient Client, string Email)> CreateUser(string name)
        {
            var client = await SecurityClient(host);
            var email = $"{Guid.NewGuid():N}@example.test";
            Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/v1/auth/register", new { name, email, password = "PasswordForTest@123", phone = "" })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { email, token = mail.Token(email) })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "PasswordForTest@123" })).StatusCode);
            return (client, email);
        }
        var (owner, _) = await CreateUser("Invitation owner");
        var (recipient, recipientEmail) = await CreateUser("Invitation recipient");
        var (other, _) = await CreateUser("Other recipient");
        using (owner) using (recipient) using (other)
        {
            var model = new iCareX.Domain.Entities.VehicleModel { Name = "Shared model", VehicleBrand = new()
            { Name = "Shared brand", VehicleType = new() { Code = Guid.NewGuid().ToString("N"), Name = "Shared type" } } };
            await using (var db = Context()) { db.VehicleModels.Add(model); await db.SaveChangesAsync(); }
            var response = await owner.PostAsJsonAsync("/api/v1/vehicles", new { name = "Invitation vehicle", plate = "51A-44444", vehicleModelId = model.Id, year = 2020, baseOdo = 0, imageUrl = "" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var vehicle = (await response.Content.ReadFromJsonAsync<VehicleResponse>())!;
            var path = $"/api/v1/vehicles/{vehicle.Id}/invitations";
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsJsonAsync(path, new { email = recipientEmail, role = "Owner" })).StatusCode);
            var invited = await owner.PostAsJsonAsync(path, new { email = recipientEmail, role = "Contributor" });
            Assert.Equal(HttpStatusCode.Created, invited.StatusCode);
            var invitation = (await invited.Content.ReadFromJsonAsync<VehicleInvitationResponse>())!;
            var token = mail.Token(recipientEmail);
            Assert.DoesNotContain(token, await invited.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.Conflict, (await owner.PostAsJsonAsync(path, new { email = recipientEmail, role = "Viewer" })).StatusCode);
            var respond = $"/api/v1/invitations/{invitation.Id}";
            Assert.Equal(HttpStatusCode.OK, (await recipient.PostAsJsonAsync(respond + "/preview", new { token })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await recipient.GetAsync($"/api/v1/vehicles/{vehicle.Id}")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await other.PostAsJsonAsync(respond + "/accept", new { token })).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await recipient.PostAsJsonAsync(respond + "/accept", new { token = "bad" })).StatusCode);
            Assert.Single((await recipient.GetFromJsonAsync<VehicleInvitationResponse[]>("/api/v1/invitations/inbox"))!);
            var accepts = await Task.WhenAll(recipient.PostAsJsonAsync(respond + "/accept", new { token }), recipient.PostAsJsonAsync(respond + "/accept", new { token }));
            Assert.Single(accepts, x => x.StatusCode == HttpStatusCode.NoContent);
            Assert.Single(accepts, x => x.StatusCode == HttpStatusCode.Conflict);
            Assert.Equal(HttpStatusCode.OK, (await recipient.GetAsync($"/api/v1/vehicles/{vehicle.Id}")).StatusCode);
            await using var fresh = Context();
            Assert.Equal(2, await fresh.VehicleMembers.CountAsync(x => x.VehicleId == vehicle.Id && x.RevokedAtUtc == null));
            Assert.NotEqual(token, (await fresh.VehicleInvitations.SingleAsync(x => x.Id == invitation.Id)).TokenHash);
        }
    }
}
