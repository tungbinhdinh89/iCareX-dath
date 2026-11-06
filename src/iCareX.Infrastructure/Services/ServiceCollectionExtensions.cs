using Microsoft.Extensions.DependencyInjection;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Shared.Contracts.Identity;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Infrastructure.Repositories;
using iCareX.Infrastructure.Services.Implementations;

namespace iCareX.Infrastructure.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<VehicleRepository>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<IVehicleCatalogService, VehicleCatalogService>();
        return services;
    }
}
