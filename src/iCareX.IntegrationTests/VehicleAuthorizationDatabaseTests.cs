using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Infrastructure.Services.Implementations;
using iCareX.Domain.Entities;
using iCareX.Shared.Contracts.Identity;
using iCareX.Shared.Contracts.Vehicles;
using Xunit;

namespace iCareX.IntegrationTests;

public sealed partial class DatabaseTests
{
    [Fact]
    public async Task RealSessionsEnforceVehicleRolesRevocationAuthorshipAndAudit()
    {
        var mail = new CapturedEmail();
        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseEnvironment("Development");
            b.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(new Dictionary<string, string?>
            { ["ConnectionStrings:iCareX"] = postgres.GetConnectionString() }));
            b.ConfigureServices(s => ConfigureEmail(s, mail));
        });
        async Task<(HttpClient Client, Guid Id)> Login(string name)
        {
            var client = factory.CreateClient();
            var token = await client.GetFromJsonAsync<CsrfResponse>("/api/v1/auth/csrf");
            client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token!.Token);
            var email = $"{Guid.NewGuid():N}@example.test";
            var registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { name, email, password = "TestPassword@123", phone = "" });
            Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
            var account = await registered.Content.ReadFromJsonAsync<AccountResponse>();
            Assert.False(account!.EmailVerified);
            Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync("/api/v1/auth/verify-email",
                new { email, token = mail.Token(email) })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "TestPassword@123" })).StatusCode);
            return (client, account!.Id);
        }
        var (owner, ownerId) = await Login("Owner");
        var (manager, managerId) = await Login("Manager");
        var (contributor, contributorId) = await Login("Contributor");
        var (viewer, viewerId) = await Login("Viewer");
        var (outsider, _) = await Login("Outsider");
        using (owner) using (manager) using (contributor) using (viewer) using (outsider)
        {
            var model = TestVehicleModel();
            await using (var db = Context()) { db.VehicleModels.Add(model); await db.SaveChangesAsync(); }
            var created = await owner.PostAsJsonAsync("/api/v1/vehicles", new { name = "Shared car", plate = "51A-12345", year = 2020, odometer = 0, vehicleModelId = model.Id, vehicleBrandId = model.VehicleBrandId, vehicleType = "Car", fuelType = "Gasoline", odometerUnit = "Kilometres", imageUrl = "" });
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            var vehicle = (await created.Content.ReadFromJsonAsync<VehicleResponse>())!;
            var url = $"/api/v1/vehicles/{vehicle.Id}";
            var fuel = new FuelEntry { VehicleId = vehicle.Id, AccountId = ownerId, CreatedByUserId = contributorId,
                DriverUserId = viewerId, Date = new(2026, 1, 1), Odo = 100, Cost = 100, Liters = 2 };
            var secondVehicle = new Vehicle { AccountId = ownerId, CreatedByUserId = ownerId,
                VehicleModelId = model.Id, VehicleBrandId = model.VehicleBrandId, VehicleType = iCareX.Domain.Enums.VehicleType.Car, FuelType = iCareX.Domain.Enums.FuelType.Gasoline, Name = "Second car", Plate = "51A-99999", PlateNormalized = "51A99999", Year = 2020 };
            await using (var db = Context())
            {
                db.Vehicles.Add(secondVehicle);
                db.VehicleMembers.AddRange(
                    new VehicleMember { VehicleId = secondVehicle.Id, UserId = ownerId, Role = VehicleMemberRole.Owner },
                    new VehicleMember { VehicleId = secondVehicle.Id, UserId = contributorId, Role = VehicleMemberRole.Contributor });
                foreach (var (id, role) in new[] { (managerId, VehicleMemberRole.Manager), (contributorId, VehicleMemberRole.Contributor), (viewerId, VehicleMemberRole.Viewer) })
                    db.VehicleMembers.Add(new VehicleMember { VehicleId = vehicle.Id, UserId = id, Role = role });
                db.FuelEntries.Add(fuel);
                await db.SaveChangesAsync();
            }
            Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync(url)).StatusCode);
            Assert.Contains(vehicle.Id, (await viewer.GetFromJsonAsync<VehicleListResponse>("/api/v1/vehicles"))!.Vehicles.Select(x => x.Id));
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(url)).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await outsider.GetAsync(url + "/members")).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await manager.GetAsync(url + "/members")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.GetAsync(url + "/members")).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PutAsJsonAsync(url, new { })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await manager.PatchAsJsonAsync(url + $"/members/{viewerId}", new { role = "Manager", version = 0 })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await owner.DeleteAsync(url + $"/members/{ownerId}?version=0")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync(url + $"/members/{viewerId}", new { role = "Owner", version = 0 })).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await owner.PatchAsJsonAsync(url + $"/members/{viewerId}", new { role = "Manager", version = 0, actorId = ownerId })).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await owner.PatchAsJsonAsync(url + $"/members/{viewerId}", new { role = "Contributor", version = 0 })).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await owner.PatchAsJsonAsync(url + $"/members/{viewerId}", new { role = "Viewer", version = 0 })).StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var access = scope.ServiceProvider.GetRequiredService<IVehicleAccessService>();
                Assert.True(await access.CanAccessResourceAsync(contributorId, new(vehicle.Id, "fuel", fuel.Id), "fuel.update", default));
                Assert.False(await access.CanAccessResourceAsync(viewerId, new(vehicle.Id, "fuel", fuel.Id), "fuel.update", default));
                Assert.False(await access.CanAccessResourceAsync(contributorId, new(Guid.NewGuid(), "fuel", fuel.Id), "fuel.update", default));
                Assert.False(await access.CanAccessResourceAsync(contributorId, new(secondVehicle.Id, "fuel", fuel.Id), "fuel.update", default));
                Assert.False(await access.CanAccessResourceAsync(ownerId, new(vehicle.Id, "maintenance", fuel.Id), "maintenance.update", default));
            }
            Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync(url + $"/members/{contributorId}?version=0")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await contributor.GetAsync(url)).StatusCode);
            Assert.DoesNotContain(vehicle.Id, (await contributor.GetFromJsonAsync<VehicleListResponse>("/api/v1/vehicles"))!.Vehicles.Select(x => x.Id));
            Assert.DoesNotContain(vehicle.Id, (await contributor.GetFromJsonAsync<CurrentUserPermissionsResponse>("/api/v1/me/permissions"))!.Vehicles.Select(x => x.VehicleId));
            // A write already waiting on the vehicle lock must re-check access after revoke commits.
            await using (var holder = Context())
            await using (var transaction = await holder.Database.BeginTransactionAsync())
            {
                await holder.Vehicles.FromSqlInterpolated($"SELECT * FROM vehicles WHERE \"Id\" = {vehicle.Id} FOR UPDATE").AsNoTracking().SingleAsync();
                var write = manager.PutAsJsonAsync(url, new { name = "Race update", plate = vehicle.Plate,
                    vehicleModelId = model.Id, vehicleBrandId = model.VehicleBrandId, vehicleType = "Car", fuelType = "Gasoline", odometerUnit = "Kilometres", year = 2020, odometer = 0, imageUrl = "", version = vehicle.Version });
                var blocked = false;
                for (var attempt = 0; attempt < 200 && !blocked; attempt++)
                {
                    await using var probe = Context();
                    blocked = await probe.Database.SqlQueryRaw<int>("SELECT COUNT(*)::int AS \"Value\" FROM pg_stat_activity WHERE datname = current_database() AND wait_event_type = 'Lock' AND query LIKE '%FOR UPDATE%'").SingleAsync() > 0;
                    if (!blocked) await Task.Delay(50);
                }
                Assert.True(blocked, "Expected the manager write to wait on the vehicle transaction lock.");
                await holder.VehicleMembers.Where(x => x.VehicleId == vehicle.Id && x.UserId == managerId && x.RevokedAtUtc == null)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, DateTimeOffset.UtcNow));
                await transaction.CommitAsync();
                Assert.Equal(HttpStatusCode.NotFound, (await write).StatusCode);
            }
            await using (var db = Context())
            {
                Assert.Equal(contributorId, (await db.FuelEntries.SingleAsync(x => x.Id == fuel.Id)).CreatedByUserId);
                Assert.Equal(1, await db.VehicleMembers.CountAsync(x => x.VehicleId == vehicle.Id && x.Role == VehicleMemberRole.Owner && x.RevokedAtUtc == null));
                Assert.True(await db.AuditLogs.AnyAsync(x => x.VehicleId == vehicle.Id && x.ActorUserId == ownerId && x.Action == "vehicle.member.revoked"));
                await db.Users.Where(x => x.Id == viewerId).ExecuteUpdateAsync(s => s.SetProperty(x => x.LockoutEnabled, true).SetProperty(x => x.LockoutEnd, DateTimeOffset.UtcNow.AddHours(1)));
            }
            Assert.Equal(HttpStatusCode.Unauthorized, (await viewer.GetAsync(url)).StatusCode);
        }
    }
}
