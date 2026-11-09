using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Domain.Authorization;

namespace iCareX.Web.Api.Authorization;

public sealed record VehiclePermissionRequirement(string Permission) : IAuthorizationRequirement;

public sealed class VehiclePermissionHandler(IVehicleAccessService access)
    : AuthorizationHandler<VehiclePermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VehiclePermissionRequirement requirement)
    {
        if (!Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return;
        if (context.Resource is VehicleResourceReference resource)
        {
            if (await access.CanAccessResourceAsync(actor, resource, requirement.Permission, CancellationToken.None))
                context.Succeed(requirement);
            return;
        }
        if (context.Resource is not HttpContext http || !Guid.TryParse(http.Request.RouteValues["vehicleId"]?.ToString(), out var vehicleId)) return;
        try
        {
            await access.RequireAsync(actor, vehicleId, requirement.Permission, http.RequestAborted);
            context.Succeed(requirement);
        }
        catch (VehicleNotFoundException) { http.Items["VehicleNotFound"] = true; }
        catch (VehicleAccessDeniedException) { }
    }
}

public sealed class VehicleAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Forbidden && context.Items.ContainsKey("VehicleNotFound"))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return Task.CompletedTask;
        }
        return fallback.HandleAsync(next, context, policy, result);
    }
}

public static class VehicleAuthorizationRegistration
{
    public static IServiceCollection AddVehicleAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("VerifiedAccount", policy => policy.RequireAuthenticatedUser().AddRequirements(new VerifiedAccountRequirement()));
            foreach (var permission in VehiclePermissions.All)
                options.AddPolicy(permission, policy => policy.RequireAuthenticatedUser().AddRequirements(new VehiclePermissionRequirement(permission)));
        });
        services.AddScoped<IAuthorizationHandler, VehiclePermissionHandler>();
        services.AddScoped<IAuthorizationHandler, VerifiedAccountHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, VehicleAuthorizationResultHandler>();
        return services;
    }
}
