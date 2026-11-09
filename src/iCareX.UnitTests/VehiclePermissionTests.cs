using iCareX.Domain.Authorization;
using iCareX.Domain.Entities;
using Xunit;

namespace iCareX.UnitTests;

public sealed class VehiclePermissionTests
{
    private static readonly Guid Actor = Guid.NewGuid();

    [Theory]
    [InlineData(VehicleMemberRole.Manager)]
    [InlineData(VehicleMemberRole.Contributor)]
    [InlineData(VehicleMemberRole.Viewer)]
    public void OnlyOwnerCanMutateMembershipOrOwnership(VehicleMemberRole role)
    {
        foreach (var permission in new[] { "vehicle.member.invite", "vehicle.member.update", "vehicle.member.remove", "vehicle.ownership.transfer", "vehicle.archive" })
        {
            Assert.False(VehiclePermissionRules.Allows(role, permission, Actor));
            Assert.True(VehiclePermissionRules.Allows(VehicleMemberRole.Owner, permission, Actor));
        }
    }

    [Theory]
    [InlineData("fuel.update")]
    [InlineData("fuel.delete")]
    [InlineData("maintenance.update")]
    [InlineData("maintenance.delete")]
    [InlineData("reminder.update")]
    [InlineData("reminder.delete")]
    public void ContributorRequiresActualCreator(string permission)
    {
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, permission, Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, permission, Actor, Guid.NewGuid(), Actor));
        Assert.True(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, permission, Actor, Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, permission, Actor, Actor, archived: true));
    }

    [Fact]
    public void ReminderAssignmentDoesNotElevateViewer()
    {
        Assert.True(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, "reminder.complete", Actor, Guid.NewGuid(), Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, "reminder.complete", Actor, Guid.NewGuid(), Guid.NewGuid()));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Viewer, "reminder.complete", Actor, Actor, Actor));
    }

    [Fact]
    public void ViewerIsReadOnlyAndUnresolvedGrantsAreDenied()
    {
        var reads = new[] { "vehicle.read", "fuel.read", "maintenance.read", "reminder.read" };
        foreach (var permission in VehiclePermissions.All)
            Assert.Equal(reads.Contains(permission), VehiclePermissionRules.Allows(VehicleMemberRole.Viewer, permission, Actor, Actor, Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, "reminder.create", Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Contributor, "maintenance_plan.read", Actor));
        Assert.False(VehiclePermissionRules.Allows((VehicleMemberRole)99, "vehicle.read", Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Owner, "vehicle.unknown", Actor));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Owner, "vehicle.read", Guid.Empty));
    }

    [Fact]
    public void ArchivedVehicleOnlyAllowsReadsAndOwnerRestore()
    {
        foreach (var permission in VehiclePermissions.All.Where(x => !x.EndsWith(".read") && x != "vehicle.restore"))
            foreach (var role in Enum.GetValues<VehicleMemberRole>())
                Assert.False(VehiclePermissionRules.Allows(role, permission, Actor, Actor, Actor, true));
        Assert.True(VehiclePermissionRules.Allows(VehicleMemberRole.Owner, "vehicle.restore", Actor, archived: true));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Manager, "vehicle.restore", Actor, archived: true));
        Assert.False(VehiclePermissionRules.Allows(VehicleMemberRole.Owner, "vehicle.restore", Actor));
    }
}
