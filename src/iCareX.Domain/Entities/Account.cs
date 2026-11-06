namespace iCareX.Domain.Entities;

public sealed class Account : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public long GarageRevision { get; set; }
}
