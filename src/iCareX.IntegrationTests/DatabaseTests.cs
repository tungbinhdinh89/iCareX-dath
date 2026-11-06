using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed class DatabaseTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
    private GarageDbContext Context() => new(new DbContextOptionsBuilder<GarageDbContext>().UseNpgsql(postgres.GetConnectionString()).Options);
    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        await using var db = Context();
        await db.Database.MigrateAsync();
    }
    public Task DisposeAsync() => postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task MigrationPersistenceConstraintsAndDeletePolicies()
    {
        await using var db = Context();
        var owner = Guid.NewGuid();
        db.Users.Add(new ApplicationUser { Id = owner, UserName = owner.ToString() });
        db.Accounts.Add(new Account { Id = owner, Name = "Test Owner" });
        var vehicle = new Vehicle { AccountId = owner, Name = "Test car", Plate = "51A-12345", PlateNormalized = "51A12345", Year = 2020 };
        db.Vehicles.Add(vehicle);
        var plan = new MaintenancePlan { AccountId = owner, VehicleId = vehicle.Id, Title = "Thay dầu", DueDate = new(2026, 1, 1), DueOdo = 1000, IntervalKm = 1000, IntervalMonths = 1 };
        db.Plans.Add(plan);
        var entry = new Entry { AccountId = owner, VehicleId = vehicle.Id, PlanId = plan.Id, Kind = EntryKind.Service, Title = "Thay dầu", Date = new(2026, 1, 1), Odo = 100, Cost = 500 };
        db.Entries.Add(entry);
        await db.SaveChangesAsync();
        await using (var fresh = Context()) Assert.Equal(500, (await fresh.Entries.SingleAsync(x => x.Id == entry.Id)).Cost);

        db.Vehicles.Add(new Vehicle { AccountId = owner, Name = "Duplicate", PlateNormalized = vehicle.PlateNormalized, Year = 2020 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var wrongOwner = new Entry { AccountId = Guid.NewGuid(), VehicleId = vehicle.Id, Kind = EntryKind.Odo, Date = new(2026, 1, 1) };
        db.Entries.Add(wrongOwner);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var invalid = new Entry { AccountId = owner, VehicleId = vehicle.Id, Kind = EntryKind.Fuel, Date = new(2026, 1, 1), Cost = 10, Liters = 0 };
        db.Entries.Add(invalid);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.Entries.Where(e => e.PlanId == plan.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.PlanId, (Guid?)null));
            await db.Plans.Where(p => p.Id == plan.Id).ExecuteDeleteAsync();
            await tx.CommitAsync();
        }
        Assert.Equal(500, (await db.Entries.SingleAsync(e => e.Id == entry.Id)).Cost);
        await db.Vehicles.Where(v => v.Id == vehicle.Id).ExecuteDeleteAsync();
        Assert.False(await db.Entries.AnyAsync(e => e.Id == entry.Id));
    }

    [Fact]
    public async Task HealthAndOpenApiAreAvailableInDevelopment()
    {
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:iCareX"] = postgres.GetConnectionString() }));
        });
        using var client = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health/ready")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/scalar/v1")).StatusCode);
    }
}
