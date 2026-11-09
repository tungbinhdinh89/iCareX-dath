namespace iCareX.Domain.Entities;

public sealed class VehicleModel : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleBrandId { get; set; }
    public string Name { get; set; } = "";
    public string NormalizedName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public VehicleBrand? VehicleBrand { get; set; }
    public ICollection<Vehicle> Vehicles { get; set; } = [];
}
