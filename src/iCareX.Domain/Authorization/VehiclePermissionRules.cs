using iCareX.Domain.Entities;

namespace iCareX.Domain.Authorization;

public static class VehiclePermissionRules
{
    // Scopes are part of the contract: a conditional grant is never an unrestricted grant.
    public static string? Scope(VehicleMemberRole role, string permission, bool archived = false)
    {
        if (!VehiclePermissions.All.Contains(permission, StringComparer.Ordinal)) return null;
        if (!Enum.IsDefined(role)) return null;
        if (archived && permission is not (VehiclePermissions.VehicleRead or VehiclePermissions.MemberRead
            or VehiclePermissions.FuelRead or VehiclePermissions.MaintenanceRead or VehiclePermissions.PlanRead
            or VehiclePermissions.ReminderRead or VehiclePermissions.VehicleRestore)) return null;
        if (permission == VehiclePermissions.VehicleRestore && !archived) return null;
        if (role == VehicleMemberRole.Owner) return "all";
        if (role == VehicleMemberRole.Manager)
            return permission is VehiclePermissions.VehicleArchive or VehiclePermissions.VehicleRestore
                or VehiclePermissions.MemberInvite or VehiclePermissions.MemberUpdate
                or VehiclePermissions.MemberRemove or VehiclePermissions.OwnershipTransfer ? null : "all";
        if (permission is VehiclePermissions.VehicleRead or VehiclePermissions.FuelRead
            or VehiclePermissions.MaintenanceRead or VehiclePermissions.ReminderRead) return "all";
        if (role == VehicleMemberRole.Viewer) return null;
        return permission switch
        {
            VehiclePermissions.FuelCreate or VehiclePermissions.MaintenanceCreate => "all",
            VehiclePermissions.FuelUpdate or VehiclePermissions.FuelDelete
                or VehiclePermissions.MaintenanceUpdate or VehiclePermissions.MaintenanceDelete
                or VehiclePermissions.ReminderUpdate or VehiclePermissions.ReminderDelete => "own",
            VehiclePermissions.ReminderComplete => "ownOrAssigned",
            // Contributor reminder.create is opt-in in the business spec; no policy enables it yet.
            _ => null
        };
    }

    public static bool Allows(VehicleMemberRole role, string permission, Guid actorId,
        Guid? creatorId = null, Guid? assignedId = null, bool archived = false) =>
        actorId != Guid.Empty && Scope(role, permission, archived) switch
        {
            "all" => true,
            "own" => creatorId == actorId,
            "ownOrAssigned" => creatorId == actorId || assignedId == actorId,
            _ => false
        };
}
