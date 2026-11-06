namespace iCareX.Domain.Entities;

public sealed class VehicleBrand : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid VehicleTypeId { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public VehicleType? VehicleType { get; set; }
    public ICollection<VehicleModel> Models { get; set; } = [];
}
