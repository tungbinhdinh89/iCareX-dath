using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using iCareX.Domain.Entities;
using iCareX.Domain.Enums;
using iCareX.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed class VehicleRegistrationMigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();
    private readonly Guid actor = Guid.NewGuid();
    private readonly Guid vehicleId = Guid.NewGuid();
    private GarageDbContext Context() => new(new DbContextOptionsBuilder<GarageDbContext>().UseNpgsql(postgres.GetConnectionString()).Options);
    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        await using var db = Context();
        await db.GetService<IMigrator>().MigrateAsync("20261109073130_FixVehicleOwnerConstraintTrigger");
        await using var tx = await db.Database.BeginTransactionAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO "AspNetUsers" ("Id", "UserName", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount")
                VALUES ({actor}, 'migration-fixture', true, false, false, false, 0);
            INSERT INTO accounts ("Id", "Name", "Phone", "GarageRevision", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                VALUES ({actor}, 'Migration fixture', '', 0, now(), now(), 0);
            INSERT INTO vehicles ("Id", "AccountId", "CreatedByUserId", "Name", "Plate", "PlateNormalized", "VehicleModelId", "Year", "BaseOdo", "ImageUrl", "CreatedAtUtc", "UpdatedAtUtc", "Version")
                VALUES ({vehicleId}, {actor}, {actor}, 'Legacy vehicle', '51G-12345', '51G12345',
                    (SELECT m."Id" FROM vehicle_models m JOIN vehicle_brands b ON m."VehicleBrandId" = b."Id"
                        JOIN vehicle_types t ON b."VehicleTypeId" = t."Id" WHERE t."Code" = 'CAR' ORDER BY m."Id" LIMIT 1),
                    2019, 12345.67, '', now(), now(), 0);
            INSERT INTO vehicle_members ("Id", "VehicleId", "UserId", "Role", "JoinedAtUtc", "CreatedAtUtc", "Version")
                VALUES ({Guid.NewGuid()}, {vehicleId}, {actor}, 'Owner', now(), now(), 0);
            """);
        await tx.CommitAsync();
    }
    public Task DisposeAsync() => postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task UpgradePreservesVehicleAndOwnerAndEnforcesNewRelationships()
    {
        await using (var db = Context()) await db.Database.MigrateAsync();
        await using (var db = Context())
        {
            var legacy = await db.Vehicles.Include(x => x.VehicleModel).ThenInclude(x => x!.VehicleBrand).SingleAsync(x => x.Id == vehicleId);
            Assert.Equal(12345.67m, legacy.Odometer);
            Assert.Equal(FuelType.Other, legacy.FuelType);
            Assert.Equal(OdometerUnit.Kilometres, legacy.OdometerUnit);
            Assert.Equal(VehicleType.Car, legacy.VehicleType);
            Assert.Equal(legacy.VehicleModel!.VehicleBrandId, legacy.VehicleBrandId);
            Assert.Equal(actor, legacy.CreatedByUserId);
            Assert.True(await db.VehicleMembers.AnyAsync(x => x.VehicleId == vehicleId && x.UserId == actor && x.Role == VehicleMemberRole.Owner && x.RevokedAtUtc == null));
            Assert.All(await db.VehicleBrands.ToArrayAsync(), x => Assert.NotEmpty(x.NormalizedName));
            Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT CASE WHEN to_regclass('public.vehicle_types') IS NULL THEN 1 ELSE 0 END AS \"Value\"").SingleAsync());
        }
        var wrongBrand = new VehicleBrand { Name = "Different brand", VehicleType = VehicleType.Car };
        Guid originalModel;
        await using (var db = Context())
        {
            originalModel = await db.Vehicles.Where(x => x.Id == vehicleId).Select(x => x.VehicleModelId).SingleAsync();
            db.VehicleBrands.Add(wrongBrand);
            await db.SaveChangesAsync();
        }
        await using (var db = Context())
        {
            db.Vehicles.Add(new Vehicle { AccountId = actor, CreatedByUserId = actor, VehicleType = VehicleType.Car,
                VehicleBrandId = wrongBrand.Id, VehicleModelId = originalModel, FuelType = FuelType.Electric });
            var error = await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
            var pg = error as Npgsql.PostgresException ?? Assert.IsType<Npgsql.PostgresException>(error.InnerException);
            Assert.Equal("23503", pg.SqlState);
        }
    }
}
