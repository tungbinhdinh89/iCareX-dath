namespace iCareX.Domain.Entities;

public sealed class Notification : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecipientUserId { get; set; }
    public Guid? VehicleId { get; set; }
    public string ReferenceType { get; set; } = "";
    public Guid? ReferenceId { get; set; }
    public string Snapshot { get; set; } = "";
    public string DeduplicationKey { get; set; } = "";
    public DateTimeOffset? ReadAtUtc { get; set; }
}
