using iCareX.Domain.Enums;

namespace iCareX.Domain.Entities;

public sealed class VehicleBrand : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public VehicleType VehicleType { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string NormalizedName { get; set; } = "";
    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<VehicleModel> Models { get; set; } = [];
}
