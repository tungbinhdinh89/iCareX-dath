using Microsoft.Extensions.DependencyInjection;
using VehicleCare.Application.Services.Contracts;
using VehicleCare.Infrastructure.Repositories;
using VehicleCare.Infrastructure.Services.Implementations;

namespace VehicleCare.Infrastructure.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<VehicleRepository>();
        services.AddScoped<IVehicleService, VehicleService>();
        return services;
    }
}
