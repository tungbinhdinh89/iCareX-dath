namespace iCareX.Domain.Authorization;

public static class VehiclePermissions
{
    public const string VehicleRead = "vehicle.read";
    public const string VehicleUpdate = "vehicle.update";
    public const string VehicleArchive = "vehicle.archive";
    public const string VehicleRestore = "vehicle.restore";
    public const string MemberRead = "vehicle.member.read";
    public const string MemberInvite = "vehicle.member.invite";
    public const string MemberUpdate = "vehicle.member.update";
    public const string MemberRemove = "vehicle.member.remove";
    public const string OwnershipTransfer = "vehicle.ownership.transfer";
    public const string FuelRead = "fuel.read";
    public const string FuelCreate = "fuel.create";
    public const string FuelUpdate = "fuel.update";
    public const string FuelDelete = "fuel.delete";
    public const string MaintenanceRead = "maintenance.read";
    public const string MaintenanceCreate = "maintenance.create";
    public const string MaintenanceUpdate = "maintenance.update";
    public const string MaintenanceDelete = "maintenance.delete";
    public const string PlanRead = "maintenance_plan.read";
    public const string PlanCreate = "maintenance_plan.create";
    public const string PlanUpdate = "maintenance_plan.update";
    public const string PlanDelete = "maintenance_plan.delete";
    public const string ReminderRead = "reminder.read";
    public const string ReminderCreate = "reminder.create";
    public const string ReminderUpdate = "reminder.update";
    public const string ReminderDelete = "reminder.delete";
    public const string ReminderComplete = "reminder.complete";

    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly(new[]
    {
        VehicleRead, VehicleUpdate, VehicleArchive, VehicleRestore,
        MemberRead, MemberInvite, MemberUpdate, MemberRemove, OwnershipTransfer,
        FuelRead, FuelCreate, FuelUpdate, FuelDelete,
        MaintenanceRead, MaintenanceCreate, MaintenanceUpdate, MaintenanceDelete,
        PlanRead, PlanCreate, PlanUpdate, PlanDelete,
        ReminderRead, ReminderCreate, ReminderUpdate, ReminderDelete, ReminderComplete
    });
}
