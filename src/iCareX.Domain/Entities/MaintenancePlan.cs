namespace iCareX.Domain.Entities;

public sealed class MaintenancePlan : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid VehicleId { get; set; }
    public string Title { get; set; } = "";
    public DateOnly DueDate { get; set; }
    public decimal DueOdo { get; set; }
    public decimal IntervalKm { get; set; }
    public int IntervalMonths { get; set; }
}
