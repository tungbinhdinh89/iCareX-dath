using Microsoft.EntityFrameworkCore;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed partial class DatabaseTests
{
    [Fact]
    public async Task OwnerConstraintAcceptsVehicleAndMemberWritesButRejectsMissingOwner()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var model = new VehicleModel { Name = "Trigger model", VehicleBrand = new()
        { Name = "Trigger brand", VehicleType = new() { Name = "Trigger type", Code = Guid.NewGuid().ToString("N") } } };
        var vehicle = new Vehicle { AccountId = ownerId, CreatedByUserId = ownerId,
            VehicleModelId = model.Id, Name = "Ford EcoSport 2019", Plate = "51G-87624", PlateNormalized = "51G87624", Year = 2019, BaseOdo = 50000 };
        var owner = new VehicleMember { VehicleId = vehicle.Id, UserId = ownerId, Role = VehicleMemberRole.Owner };
        var viewer = new VehicleMember { VehicleId = vehicle.Id, UserId = viewerId, Role = VehicleMemberRole.Viewer };
        await using (var db = Context())
        {
            db.Users.AddRange(new ApplicationUser { Id = ownerId, UserName = ownerId.ToString() }, new ApplicationUser { Id = viewerId, UserName = viewerId.ToString() });
            db.Accounts.AddRange(new Account { Id = ownerId, Name = "Owner" }, new Account { Id = viewerId, Name = "Viewer" });
            db.VehicleModels.Add(model);
            db.Vehicles.Add(vehicle);
            db.VehicleMembers.AddRange(owner, viewer);
            await db.SaveChangesAsync(); // Deferred triggers must handle both table row types at commit.
            vehicle.Name = "Updated vehicle";
            vehicle.Version++;
            await db.SaveChangesAsync();
            vehicle.ArchivedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync();
            vehicle.ArchivedAtUtc = null;
            await db.SaveChangesAsync();
            viewer.Role = VehicleMemberRole.Contributor;
            await db.SaveChangesAsync();
            db.VehicleMembers.Remove(viewer);
            await db.SaveChangesAsync(); // DELETE must use OLD.VehicleId.
        }
        await using (var db = Context())
        {
            var member = await db.VehicleMembers.SingleAsync(x => x.Id == owner.Id);
            member.RevokedAtUtc = DateTimeOffset.UtcNow;
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
            var postgres = exception as Npgsql.PostgresException ?? Assert.IsType<Npgsql.PostgresException>(exception.InnerException);
            Assert.Equal("23514", postgres.SqlState);
        }
        await using (var db = Context())
        {
            db.Vehicles.Add(new Vehicle { AccountId = ownerId, CreatedByUserId = ownerId,
                VehicleModelId = model.Id, Name = "Missing Owner", PlateNormalized = "51G00001", Year = 2019 });
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
            var postgres = exception as Npgsql.PostgresException ?? Assert.IsType<Npgsql.PostgresException>(exception.InnerException);
            Assert.Equal("23514", postgres.SqlState);
        }
        await using (var db = Context())
        {
            var destination = new Vehicle { AccountId = ownerId, CreatedByUserId = ownerId,
                VehicleModelId = model.Id, Name = "Move destination", PlateNormalized = "51G00002", Year = 2019 };
            db.Vehicles.Add(destination);
            (await db.VehicleMembers.SingleAsync(x => x.Id == owner.Id)).VehicleId = destination.Id;
            // Destination has one Owner, but moving it must not leave the source without one.
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
            var postgres = exception as Npgsql.PostgresException ?? Assert.IsType<Npgsql.PostgresException>(exception.InnerException);
            Assert.Equal("23514", postgres.SqlState);
        }
        await using (var db = Context())
        {
            Assert.Equal("Updated vehicle", (await db.Vehicles.SingleAsync(x => x.Id == vehicle.Id)).Name);
            Assert.Null((await db.VehicleMembers.SingleAsync(x => x.Id == owner.Id)).RevokedAtUtc);
            Assert.Equal(vehicle.Id, (await db.VehicleMembers.SingleAsync(x => x.Id == owner.Id)).VehicleId);
        }
    }
}
