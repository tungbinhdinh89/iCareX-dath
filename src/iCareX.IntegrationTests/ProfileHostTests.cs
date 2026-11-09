using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Shared.Contracts.Identity;
using Xunit;

namespace iCareX.IntegrationTests;

[Trait("Category", "Host")]
public sealed class ProfileHostTests
{
    private static readonly Guid Actor = Guid.NewGuid();

    [Fact]
    public async Task ProfileRequiresAuthenticationCsrfAndStrictDtoAndUsesAuthenticatedActor()
    {
        var service = new ProfileDouble();
        using var host = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:iCareX"] = "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1" }));
            b.ConfigureServices(s =>
            {
                s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
                s.AddAuthentication(o => { o.DefaultAuthenticateScheme = "ProfileTest"; o.DefaultChallengeScheme = "ProfileTest"; })
                    .AddScheme<AuthenticationSchemeOptions, ProfileAuthentication>("ProfileTest", _ => { });
                s.RemoveAll<IAuthService>();
                s.AddSingleton<IAuthService>(service);
            });
        });
        using var client = host.CreateClient();
        var request = new { name = "Tên mới", phone = "0912345678" };
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/v1/auth/me", request)).StatusCode);
        client.DefaultRequestHeaders.Add("X-Test-Authenticated", "true");
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/auth/me", request)).StatusCode);
        var csrf = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        foreach (var field in new[] { "userId", "accountId", "email", "roles", "emailVerified", "accountStatus", "password" })
        {
            var forged = new Dictionary<string, object?> { ["name"] = request.name, ["phone"] = request.phone, [field] = "forged" };
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync("/api/v1/auth/me", forged)).StatusCode);
        }
        Assert.Null(service.LastActor);
        var response = await client.PutAsJsonAsync("/api/v1/auth/me", request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        Assert.Equal(Actor, service.LastActor);
        var account = (await response.Content.ReadFromJsonAsync<AccountResponse>())!;
        Assert.Equal(request.name, account.Name);
        Assert.Equal("original@example.test", account.Email);
    }

    private sealed class ProfileAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(
            Request.Headers.ContainsKey("X-Test-Authenticated") ? AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Actor.ToString())], "ProfileTest")), "ProfileTest"))
                : AuthenticateResult.NoResult());
    }

    private sealed class ProfileDouble : IAuthService
    {
        public Guid? LastActor { get; private set; }
        public Task<AccountResponse?> UpdateProfileAsync(Guid accountId, UpdateProfileRequest request, CancellationToken ct)
        {
            LastActor = accountId;
            return Task.FromResult<AccountResponse?>(new(accountId, request.Name, "original@example.test", request.Phone ?? "", ["User"], "PendingVerification", false));
        }
        public Task<RegistrationResponse> RegisterAsync(RegisterRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<(AccountResponse Account, Guid SessionId)?> LoginAsync(LoginRequest request, CancellationToken ct) => throw new NotSupportedException();
        public Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken ct) => throw new NotSupportedException();
        public Task RevokeSessionAsync(Guid accountId, Guid sessionId, CancellationToken ct) => throw new NotSupportedException();
    }
}
