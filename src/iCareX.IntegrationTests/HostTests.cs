using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace iCareX.IntegrationTests;

[Trait("Category", "Host")]
public sealed class HostTests
{
    private static WebApplicationFactory<Program> Host(string environment) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b =>
        {
            b.UseEnvironment(environment);
            b.ConfigureServices(s => s.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider()));
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:iCareX"] = "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1",
                ["AccountEmail:Host"] = "",
                ["AccountEmail:PublicBaseUrl"] = ""
            }));
        });

    [Fact]
    public async Task LivenessAndDocumentationWorkWithoutDatabaseAndReadinessFailsSafely()
    {
        using var factory = Host("Development");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        var readiness = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.ServiceUnavailable, readiness.StatusCode);
        Assert.Equal("Unhealthy", await readiness.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scalar/v1")).StatusCode);
    }

    [Fact]
    public async Task ProductionDoesNotExposeDevelopmentDocumentation()
    {
        using var factory = Host("Production");
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/scalar/v1")).StatusCode);
    }

    [Fact]
    public async Task AuthenticationEndpointsRequireCsrfAndAuthentication()
    {
        using var factory = Host("Development");
        using var client = factory.CreateClient();

        var csrf = await client.GetAsync("/api/v1/auth/csrf");
        Assert.Equal(HttpStatusCode.OK, csrf.StatusCode);
        Assert.Contains("\"token\"", await csrf.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            name = "Nguyen Van A",
            email = "a@example.com",
            password = "Password@123",
            phone = ""
        });
        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);

        Assert.Equal(HttpStatusCode.Unauthorized,
            (await client.GetAsync("/api/v1/auth/me")).StatusCode);
        foreach (var path in new[] { "/api/v1/me/permissions", "/api/v1/vehicles", $"/api/v1/vehicles/{Guid.NewGuid()}/permissions", $"/api/v1/vehicles/{Guid.NewGuid()}/members" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(path)).StatusCode);
        foreach (var (path, body) in new (string, object)[]
        {
            ("verification-email", new { email = "a@example.test" }),
            ("verify-email", new { email = "a@example.test", token = "invalid" }),
            ("forgot-password", new { email = "a@example.test" }),
            ("reset-password", new { email = "a@example.test", token = "invalid", newPassword = "Password@123456" })
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync($"/api/v1/auth/{path}", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/v1/auth/change-password", new { currentPassword = "Password@123456", newPassword = "Password@123457" })).StatusCode);
    }

    [Fact]
    public async Task UnconfiguredMailFailsSafelyAndRecoveryIsRateLimited()
    {
        using var factory = Host("Development");
        using var client = factory.CreateClient();
        var token = await client.GetFromJsonAsync<iCareX.Shared.Contracts.Identity.CsrfResponse>("/api/v1/auth/csrf");
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token!.Token);
        for (var request = 0; request < 20; request++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "unknown@example.test" });
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.True(response.Headers.CacheControl?.NoStore);
        }
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "unknown@example.test" })).StatusCode);
    }
}
