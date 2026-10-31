using Microsoft.Extensions.DependencyInjection;

namespace VehicleCare.Application.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        return services;
    }
}