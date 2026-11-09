namespace iCareX.Domain.Entities;

public sealed class Vehicle : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = "";
    public string Plate { get; set; } = "";
    public string PlateNormalized { get; set; } = "";
    public Guid VehicleModelId { get; set; }
    public int Year { get; set; }
    public decimal BaseOdo { get; set; }
    public string ImageUrl { get; set; } = "";
    public DateTimeOffset? ArchivedAtUtc { get; set; }
    public VehicleModel? VehicleModel { get; set; }
}
