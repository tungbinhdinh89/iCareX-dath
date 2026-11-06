using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;
using Serilog.Events;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;

var action = args.FirstOrDefault() ?? "check";
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithProperty("Application", "iCareX.DatabaseTool")
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "logs", "icarex-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateLogger();
try
{
    Log.Information("Database action started: {Action}", action);
    var configuration = new ConfigurationBuilder()
        .AddUserSecrets(typeof(Program).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();
    var connectionString = (configuration.GetConnectionString("iCareX") ?? configuration.GetConnectionString("VehicleCare"))
        ?? throw new InvalidOperationException(
            "Missing ConnectionStrings:iCareX. Configure it with dotnet user-secrets.");
    var settings = new NpgsqlConnectionStringBuilder(connectionString);
    if (action == "provision")
    {
        var target = settings.Database;
        if (target is not ("icarex" or "vehiclecare")) throw new InvalidOperationException("Provisioning only supports icarex or the existing vehiclecare database.");
        settings.Database = "postgres";
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();
        await using var exists = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", admin);
        exists.Parameters.AddWithValue("name", target);
        if (await exists.ExecuteScalarAsync() is null)
        {
            await using var create = new NpgsqlCommand(target == "icarex" ? "CREATE DATABASE icarex" : "CREATE DATABASE vehiclecare", admin);
            await create.ExecuteNonQueryAsync();
            Log.Information("Database {Database} created.", target);
        }
        else Log.Information("Database {Database} already exists; left unchanged.", target);
        return;
    }
    await using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    await using var query = new NpgsqlCommand("SELECT current_database(), version(), COALESCE((SELECT ssl FROM pg_stat_ssl WHERE pid = pg_backend_pid()), false)", connection);
    await using (var reader = await query.ExecuteReaderAsync())
    {
        await reader.ReadAsync();
        Log.Information("Database check succeeded for {Database}; server {Server}; TLS {Tls}",
            reader.GetString(0), reader.GetString(1), reader.GetBoolean(2));
    }
    if (action == "check") return;
    if (action != "smoke") throw new InvalidOperationException("Unknown action.");
    var options = new DbContextOptionsBuilder<GarageDbContext>().UseNpgsql(connectionString).Options;
    var id = Guid.NewGuid();
    // Persist a disposable fixture, read it in a new context, then remove only its unique identity row.
    try
    {
        await using (var db = new GarageDbContext(options))
        {
            db.Users.Add(new ApplicationUser { Id = id, UserName = $"smoke-{id:N}", NormalizedUserName = $"SMOKE-{id:N}" });
            db.Accounts.Add(new Account { Id = id, Name = "Connection smoke fixture" });
            db.Settings.Add(new UserSettings { AccountId = id });
            await db.SaveChangesAsync();
        }
        await using (var db = new GarageDbContext(options))
        {
            if (!await db.Accounts.AnyAsync(a => a.Id == id)) throw new InvalidOperationException("Persistence check failed.");
        }
        Log.Information("Persistence verified across separate DbContexts.");
    }
    finally
    {
        await using var cleanup = new GarageDbContext(options);
        await cleanup.Users.Where(u => u.Id == id).ExecuteDeleteAsync();
    }
    Log.Information("Smoke fixture removed.");
}
catch (PostgresException e)
{
    Log.Error(e, "Database operation failed (SQLSTATE {SqlState}); check permissions/schema. No credentials printed.",
        e.SqlState);
    Environment.ExitCode = 1;
}
catch (Exception e)
{
    Log.Error(e, "Database operation failed ({ExceptionType}); check configuration and connectivity. No credentials printed.",
        e.GetType().Name);
    Environment.ExitCode = 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}
