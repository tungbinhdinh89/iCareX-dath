namespace iCareX.Domain.Entities;

public sealed class UserSettings : AuditedEntity
{
    public Guid AccountId { get; set; }
    public bool Reminders { get; set; } = true;
    public decimal ThresholdKm { get; set; } = 500;
    public int ThresholdDays { get; set; } = 30;
}
