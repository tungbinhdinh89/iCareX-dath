namespace iCareX.Domain.Entities;

public sealed class OdometerReading : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? DriverUserId { get; set; }
    public DateOnly Date { get; set; }
    public decimal Odo { get; set; }
    public string Note { get; set; } = "";
}
