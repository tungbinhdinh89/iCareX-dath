using iCareX.Domain.Enums;

namespace iCareX.Domain.Entities;

public sealed class Vehicle : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public VehicleType VehicleType { get; set; }
    public Guid VehicleBrandId { get; set; }
    public string? Name { get; set; }
    public string? Plate { get; set; }
    public string? PlateNormalized { get; set; }
    public Guid VehicleModelId { get; set; }
    public int? Year { get; set; }
    public FuelType FuelType { get; set; }
    public decimal? FuelCapacity { get; set; }
    public decimal Odometer { get; set; }
    public OdometerUnit OdometerUnit { get; set; } = OdometerUnit.Kilometres;
    public string? VIN { get; set; }
    public string? RegistrationId { get; set; }
    public decimal? PurchasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public string? Notes { get; set; }
    public string? ImageUrl { get; set; }
    public DateTimeOffset? ArchivedAtUtc { get; set; }
    public VehicleModel? VehicleModel { get; set; }
    public VehicleBrand? VehicleBrand { get; set; }
}
