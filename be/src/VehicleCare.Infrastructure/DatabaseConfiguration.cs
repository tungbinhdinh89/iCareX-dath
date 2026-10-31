using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VehicleCare.Application;

namespace VehicleCare.Infrastructure;

public static class DatabaseConfiguration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseOptions>().Configure<IConfiguration>((options, configuration) =>
            options.ConnectionString = configuration.GetConnectionString("VehicleCare") ?? "")
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Missing ConnectionStrings:VehicleCare. Configure it with dotnet user-secrets.")
            .ValidateOnStart();
        services.AddDbContext<GarageDbContext>((provider, options) =>
            Configure(options, provider.GetRequiredService<IOptions<DatabaseOptions>>().Value.ConnectionString));
        services.AddSingleton<IBusinessClock, BusinessClock>();
        return services;
    }

    public static void Configure(DbContextOptionsBuilder options, string connection) => options.UseNpgsql(connection);
}

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = "";
}

public sealed class GarageDbContextFactory : IDesignTimeDbContextFactory<GarageDbContext>
{
    public GarageDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddUserSecrets(typeof(DatabaseConfiguration).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("VehicleCare");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException(
                "Missing ConnectionStrings:VehicleCare. Configure it with dotnet user-secrets.");
        var options = new DbContextOptionsBuilder<GarageDbContext>();
        DatabaseConfiguration.Configure(options, connection);
        return new(options.Options);
    }
}

public sealed class BusinessClock : IBusinessClock
{
    private static readonly TimeZoneInfo Vietnam = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, Vietnam).DateTime);
}
