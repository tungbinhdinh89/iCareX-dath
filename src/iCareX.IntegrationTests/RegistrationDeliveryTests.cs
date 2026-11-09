using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Shared.Contracts.Identity;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed partial class DatabaseTests
{
    private sealed class RecoverableEmail(CapturedEmail delivered) : IAccountEmailSender
    {
        public string Failure { get; set; } = "";
        public Func<string, Task>? BeforeSend { get; set; }
        public void EnsureConfigured()
        {
            if (Failure == "configuration") throw new EmailDeliveryUnavailableException("invalid_smtp_configuration");
        }
        public async Task SendAsync(string recipient, string subject, string text, CancellationToken ct)
        {
            if (BeforeSend is not null) await BeforeSend(recipient);
            if (Failure == "transport") throw new EmailDeliveryUnavailableException("smtp_send_failed");
            await delivered.SendAsync(recipient, subject, text, ct);
        }
    }

    [Theory]
    [InlineData("configuration")]
    [InlineData("transport")]
    [InlineData("")]
    public async Task RegistrationCommitsBeforeDeliveryAndFailedEmailCanBeRetried(string failure)
    {
        var mail = new CapturedEmail();
        var sender = new RecoverableEmail(mail) { Failure = failure };
        sender.BeforeSend = async address =>
        {
            await using var fresh = Context();
            var user = await fresh.Users.SingleAsync(x => x.Email == address);
            Assert.True(await fresh.Accounts.AnyAsync(x => x.Id == user.Id));
            Assert.True(await fresh.Settings.AnyAsync(x => x.AccountId == user.Id));
        };
        using var baseHost = SecurityHost(mail);
        using var host = baseHost.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IAccountEmailSender>();
            s.AddSingleton<IAccountEmailSender>(sender);
        }));
        using var client = await SecurityClient(host);
        var email = $"{Guid.NewGuid():N}@example.test";
        var request = new { name = "Durable registration", email, password = "TestPassword@123", phone = "" };
        var created = await client.PostAsJsonAsync("/api/v1/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var response = (await created.Content.ReadFromJsonAsync<RegistrationResponse>())!;
        Assert.Equal(failure.Length == 0, response.VerificationEmailSent);
        Assert.False(response.EmailVerified);
        Assert.Equal("PendingVerification", response.AccountStatus);
        await using (var fresh = Context())
        {
            Assert.True(await fresh.Accounts.AnyAsync(x => x.Id == response.Id));
            Assert.False((await fresh.Users.SingleAsync(x => x.Id == response.Id)).EmailConfirmed);
            Assert.True(await fresh.Settings.AnyAsync(x => x.AccountId == response.Id));
        }
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/v1/auth/register", request)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = request.password })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/api/v1/vehicles")).StatusCode);
        sender.Failure = "";
        Assert.Equal(HttpStatusCode.Accepted, (await client.PostAsJsonAsync("/api/v1/auth/verification-email", new { email })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { email, token = mail.Token(email) })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/v1/vehicles")).StatusCode);
    }
}
