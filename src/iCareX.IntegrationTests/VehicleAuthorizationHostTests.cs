using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Domain.Authorization;
using iCareX.Domain.Entities;
using iCareX.Shared.Contracts.Identity;
using iCareX.Shared.Contracts.Vehicles;
using Xunit;

namespace iCareX.IntegrationTests;

// HTTP middleware/controller contract checks with a server-side access double.
// Persistence, cookie sessions and concurrency are covered separately by PostgreSQL tests.
[Trait("Category", "Host")]
public sealed class VehicleAuthorizationHostTests
{
    [Theory]
    [InlineData(VehicleMemberRole.Owner, 200)]
    [InlineData(VehicleMemberRole.Manager, 200)]
    [InlineData(VehicleMemberRole.Contributor, 403)]
    [InlineData(VehicleMemberRole.Viewer, 403)]
    [InlineData(null, 404)]
    public async Task MembershipListUsesCurrentVehicleRole(VehicleMemberRole? role, int expected)
    {
        using var host = Host(role);
        using var client = host.CreateClient();
        var response = await client.GetAsync($"/api/v1/vehicles/{Guid.NewGuid()}/members");
        Assert.Equal(expected, (int)response.StatusCode);
    }

    [Fact]
    public async Task OnlyOwnerCanChangeRolesAndCsrfAndStrictDtoAreRequired()
    {
        var url = $"/api/v1/vehicles/{Guid.NewGuid()}/members/{Guid.NewGuid()}";
        using var host = Host(VehicleMemberRole.Owner);
        using var owner = host.CreateClient();
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync(url, new { role = "Manager", version = 0 })).StatusCode);
        var csrf = await owner.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
        owner.DefaultRequestHeaders.Add("X-CSRF-TOKEN", csrf!.Token);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync(url, new { role = "Manager", version = 0, userId = Guid.NewGuid() })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync(url, new { role = "Manager", version = 0 })).StatusCode);
        using var managerHost = Host(VehicleMemberRole.Manager);
        using var manager = managerHost.CreateClient();
        Assert.Equal(HttpStatusCode.Forbidden, (await manager.PatchAsJsonAsync(url, new { role = "Owner", version = 0 })).StatusCode);
    }

    private static WebApplicationFactory<Program> Host(VehicleMemberRole? role) => new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
    {
        b.UseEnvironment("Development");
        b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
        { ["ConnectionStrings:iCareX"] = "Host=127.0.0.1;Port=1;Database=unavailable;Username=test;Password=test;Timeout=1" }));
        b.ConfigureServices(services =>
        {
            services.AddAuthentication(o => { o.DefaultAuthenticateScheme = "Test"; o.DefaultChallengeScheme = "Test"; o.DefaultForbidScheme = "Test"; })
                .AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
            services.RemoveAll<IVehicleAccessService>();
            services.AddSingleton<IVehicleAccessService>(new AccessDouble(role));
            services.RemoveAll<IVehicleMemberService>();
            services.AddSingleton<IVehicleMemberService, MembersDouble>();
        });
    });

    private sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, "b192b10a-4005-40ef-b650-4dcfcb503172"),
                new Claim(ClaimTypes.Role, "SystemAdmin")], "Test")), "Test")));
    }

    private sealed class AccessDouble(VehicleMemberRole? role) : IVehicleAccessService
    {
        public Task<VehicleMembership> RequireAsync(Guid userId, Guid vehicleId, string permission, CancellationToken ct)
        {
            if (role is null) throw new VehicleNotFoundException();
            if (!VehiclePermissionRules.Allows(role.Value, permission, userId)) throw new VehicleAccessDeniedException();
            return Task.FromResult(new VehicleMembership(vehicleId, userId, userId, role.Value, false));
        }
        public Task<bool> CanAccessResourceAsync(Guid userId, VehicleResourceReference resource, string permission, CancellationToken ct) => throw new NotSupportedException();
        public Task<VehiclePermissionsResponse> GetPermissionsAsync(Guid userId, Guid vehicleId, CancellationToken ct) => throw new NotSupportedException();
        public Task<CurrentUserPermissionsResponse> GetCurrentPermissionsAsync(Guid userId, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class MembersDouble : IVehicleMemberService
    {
        public Task<IReadOnlyCollection<VehicleMemberResponse>> ListAsync(Guid actorId, Guid vehicleId, CancellationToken ct) =>
            Task.FromResult<IReadOnlyCollection<VehicleMemberResponse>>([]);
        public Task<VehicleMemberResponse> UpdateAsync(Guid actorId, Guid vehicleId, Guid userId, UpdateVehicleMemberRequest request, CancellationToken ct) =>
            Task.FromResult(new VehicleMemberResponse(userId, "Member", request.Role, DateTimeOffset.UtcNow, request.Version + 1));
        public Task RevokeAsync(Guid actorId, Guid vehicleId, Guid userId, long version, CancellationToken ct) => Task.CompletedTask;
    }
}
