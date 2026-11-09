using Microsoft.EntityFrameworkCore;
using iCareX.Application.Abstractions;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class VehicleMembershipResolver(GarageDbContext db, IBusinessClock clock) : IVehicleMembershipResolver
{
    public Task<VehicleMembership?> ResolveAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken) =>
        (from member in db.VehicleMembers.AsNoTracking()
         join vehicle in db.Vehicles.AsNoTracking() on member.VehicleId equals vehicle.Id
         join user in db.Users.AsNoTracking() on member.UserId equals user.Id
         where member.UserId == userId && member.VehicleId == vehicleId && member.RevokedAtUtc == null && user.EmailConfirmed
             && (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= clock.UtcNow)
         select new VehicleMembership(vehicle.Id, userId, vehicle.AccountId, member.Role, vehicle.ArchivedAtUtc != null))
        .SingleOrDefaultAsync(cancellationToken);
}
