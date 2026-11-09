using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Domain.Authorization;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;
using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class VehicleAccessService(
    GarageDbContext db, IVehicleMembershipResolver memberships, UserManager<ApplicationUser> users) : IVehicleAccessService
{
    public async Task<VehicleMembership> RequireAsync(Guid userId, Guid vehicleId, string permission, CancellationToken cancellationToken)
    {
        var member = await memberships.ResolveAsync(userId, vehicleId, cancellationToken)
            ?? throw new VehicleNotFoundException();
        if (!VehiclePermissionRules.Allows(member.Role, permission, userId, archived: member.IsArchived))
            throw new VehicleAccessDeniedException();
        return member;
    }

    public async Task<bool> CanAccessResourceAsync(Guid userId, VehicleResourceReference resource,
        string permission, CancellationToken cancellationToken)
    {
        var member = await memberships.ResolveAsync(userId, resource.VehicleId, cancellationToken);
        if (member is null) return false;
        // Each operation is bound to its resource kind, even for an Owner.
        if (resource.Kind == "fuel" && permission.StartsWith("fuel.", StringComparison.Ordinal))
        {
            var record = await db.FuelEntries.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == resource.Id && x.VehicleId == resource.VehicleId, cancellationToken);
            return record is not null && VehiclePermissionRules.Allows(member.Role, permission, userId,
                record.CreatedByUserId, archived: member.IsArchived);
        }
        if (resource.Kind == "maintenance" && permission.StartsWith("maintenance.", StringComparison.Ordinal))
        {
            var record = await db.MaintenanceRecords.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == resource.Id && x.VehicleId == resource.VehicleId, cancellationToken);
            return record is not null && VehiclePermissionRules.Allows(member.Role, permission, userId,
                record.CreatedByUserId, archived: member.IsArchived);
        }
        if (resource.Kind == "reminder" && permission.StartsWith("reminder.", StringComparison.Ordinal))
        {
            var record = await db.Reminders.AsNoTracking().SingleOrDefaultAsync(
                x => x.Id == resource.Id && x.VehicleId == resource.VehicleId, cancellationToken);
            return record is not null && VehiclePermissionRules.Allows(member.Role, permission, userId,
                record.CreatedByUserId, record.AssignedToUserId, member.IsArchived);
        }
        if (resource.Kind == "maintenance_plan" && permission.StartsWith("maintenance_plan.", StringComparison.Ordinal))
            return await db.Plans.AsNoTracking().AnyAsync(x => x.Id == resource.Id && x.VehicleId == resource.VehicleId, cancellationToken)
                && VehiclePermissionRules.Allows(member.Role, permission, userId, archived: member.IsArchived);
        return false;
    }

    public async Task<VehiclePermissionsResponse> GetPermissionsAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var member = await memberships.ResolveAsync(userId, vehicleId, cancellationToken)
            ?? throw new VehicleNotFoundException();
        return Projection(member);
    }

    public async Task<CurrentUserPermissionsResponse> GetCurrentPermissionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId.ToString()) ?? throw new VehicleAccessDeniedException();
        var roles = (await users.GetRolesAsync(user)).Where(x => x is GlobalRoles.User or GlobalRoles.SystemAdmin).ToArray();
        var ids = await db.VehicleMembers.AsNoTracking().Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .OrderBy(x => x.VehicleId).Select(x => x.VehicleId).ToArrayAsync(cancellationToken);
        var results = new List<VehiclePermissionsResponse>();
        foreach (var id in ids)
        {
            var member = await memberships.ResolveAsync(userId, id, cancellationToken);
            if (member is not null) results.Add(Projection(member));
        }
        return new(userId, roles, results);
    }

    private static VehiclePermissionsResponse Projection(VehicleMembership member) =>
        new(member.VehicleId, member.Role.ToString(), member.Role == VehicleMemberRole.Owner ? "Owned" : "Shared",
            member.IsArchived, VehiclePermissions.All
                .Select(code => new VehiclePermissionGrant(code, VehiclePermissionRules.Scope(member.Role, code, member.IsArchived)!))
                .Where(x => x.Scope is not null).ToArray());
}
