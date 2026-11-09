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

public sealed partial class DatabaseTests : IAsyncLifetime
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
        var model = TestVehicleModel();
        db.VehicleModels.Add(model);
        var vehicle = new Vehicle { VehicleModelId = model.Id, VehicleBrandId = model.VehicleBrandId, VehicleType = iCareX.Domain.Enums.VehicleType.Car, FuelType = iCareX.Domain.Enums.FuelType.Gasoline, AccountId = owner, CreatedByUserId = owner, Name = "Test car", Plate = "51A-12345", PlateNormalized = "51A12345", Year = 2020 };
        db.Vehicles.Add(vehicle);
        db.VehicleMembers.Add(new VehicleMember { VehicleId = vehicle.Id, UserId = owner, Role = VehicleMemberRole.Owner });
        var plan = new MaintenancePlan { AccountId = owner, VehicleId = vehicle.Id, Title = "Thay dầu", DueDate = new(2026, 1, 1), DueOdo = 1000, IntervalKm = 1000, IntervalMonths = 1 };
        db.Plans.Add(plan);
        var entry = new MaintenanceRecord { AccountId = owner, VehicleId = vehicle.Id, PlanId = plan.Id, CreatedByUserId = owner, Title = "Thay dầu", Date = new(2026, 1, 1), Odo = 100, Cost = 500 };
        db.MaintenanceRecords.Add(entry);
        await db.SaveChangesAsync();
        await using (var fresh = Context()) Assert.Equal(500, (await fresh.MaintenanceRecords.SingleAsync(x => x.Id == entry.Id)).Cost);
        await using (var fresh = Context())
            Assert.Equal(1, await fresh.VehicleMembers.CountAsync(x => x.VehicleId == vehicle.Id &&
                x.Role == VehicleMemberRole.Owner && x.RevokedAtUtc == null));

        var invitation = new VehicleInvitation
        {
            VehicleId = vehicle.Id, InvitedByUserId = owner, InvitedEmail = "driver@example.test",
            NormalizedInvitedEmail = "DRIVER@EXAMPLE.TEST", TokenHash = "hash-only-value",
            Role = VehicleMemberRole.Viewer, ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7)
        };
        db.VehicleInvitations.Add(invitation);
        db.Reminders.Add(new Reminder
        {
            VehicleId = vehicle.Id, CreatedByUserId = owner, Title = "Bảo dưỡng",
            DueOdo = 2000
        });
        db.Notifications.Add(new Notification
        {
            RecipientUserId = owner, VehicleId = vehicle.Id, ReferenceType = "Reminder",
            ReferenceId = invitation.Id, Snapshot = """{"title":"Bảo dưỡng"}""",
            DeduplicationKey = "reminder-1"
        });
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = owner, VehicleId = vehicle.Id, Action = "vehicle.created",
            EntityType = "Vehicle", EntityId = vehicle.Id,
            OldValues = null, NewValues = """{"name":"Test car"}"""
        });
        await db.SaveChangesAsync();
        Assert.DoesNotContain(typeof(VehicleInvitation).GetProperties(),
            property => property.Name.Equals("Token", StringComparison.OrdinalIgnoreCase));

        db.VehicleInvitations.Add(new VehicleInvitation
        {
            VehicleId = vehicle.Id, InvitedByUserId = owner, InvitedEmail = "other@example.test",
            NormalizedInvitedEmail = invitation.NormalizedInvitedEmail, TokenHash = "another-hash",
            Role = VehicleMemberRole.Viewer, ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7)
        });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.VehicleMembers.Add(new VehicleMember { VehicleId = vehicle.Id, UserId = owner, Role = VehicleMemberRole.Owner });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();

        db.Vehicles.Add(new Vehicle { AccountId = owner, CreatedByUserId = owner, Name = "Duplicate", VehicleModelId = model.Id, VehicleBrandId = model.VehicleBrandId, VehicleType = iCareX.Domain.Enums.VehicleType.Car, FuelType = iCareX.Domain.Enums.FuelType.Gasoline, Plate = vehicle.Plate, PlateNormalized = vehicle.PlateNormalized, Year = 2020 });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var wrongOwner = new OdometerReading { AccountId = Guid.NewGuid(), VehicleId = vehicle.Id, CreatedByUserId = owner, Date = new(2026, 1, 1) };
        db.OdometerReadings.Add(wrongOwner);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        var invalid = new FuelEntry { AccountId = owner, VehicleId = vehicle.Id, CreatedByUserId = owner, Date = new(2026, 1, 1), Cost = 10, Liters = 0 };
        db.FuelEntries.Add(invalid);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        await using (var tx = await db.Database.BeginTransactionAsync())
        {
            await db.MaintenanceRecords.Where(e => e.PlanId == plan.Id).ExecuteUpdateAsync(s => s.SetProperty(e => e.PlanId, (Guid?)null));
            await db.Plans.Where(p => p.Id == plan.Id).ExecuteDeleteAsync();
            await tx.CommitAsync();
        }
        Assert.Equal(500, (await db.MaintenanceRecords.SingleAsync(e => e.Id == entry.Id)).Cost);
        // Invitations/reminders/audit retain vehicle history; physical deletion is restricted.
        var deletion = await Assert.ThrowsAsync<Npgsql.PostgresException>(() => db.Vehicles.Where(v => v.Id == vehicle.Id).ExecuteDeleteAsync());
        Assert.Equal("23503", deletion.SqlState);
        Assert.True(await db.MaintenanceRecords.AnyAsync(e => e.Id == entry.Id));
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
