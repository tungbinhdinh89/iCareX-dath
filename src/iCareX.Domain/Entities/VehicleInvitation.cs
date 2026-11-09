namespace iCareX.Domain.Entities;

public sealed class VehicleInvitation : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleId { get; set; }
    public Guid InvitedByUserId { get; set; }
    public string InvitedEmail { get; set; } = "";
    public string NormalizedInvitedEmail { get; set; } = "";
    public string TokenHash { get; set; } = "";
    public VehicleMemberRole Role { get; set; }
    public VehicleInvitationStatus Status { get; set; } = VehicleInvitationStatus.Pending;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public Guid? AcceptedByUserId { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
}
