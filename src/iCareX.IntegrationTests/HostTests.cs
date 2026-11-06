using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace iCareX.IntegrationTests;

[Trait("Category", "Host")]
public sealed class HostTests
{
    private static WebApplicationFactory<Program> Host(string environment) => new WebApplicationFactory<Program>()
        .WithWebHostBuilder(b =>
        {
            b.UseEnvironment(environment);
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:iCareX"] = "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1"
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
    }
}
