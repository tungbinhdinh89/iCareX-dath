namespace iCareX.Domain.Entities;

public sealed class VehicleType : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public ICollection<VehicleBrand> Brands { get; set; } = [];
}
