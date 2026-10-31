namespace VehicleCare.Domain;

public enum VehicleType { Car, Motorcycle }
public enum EntryKind { Fuel, Service, Odo }

public abstract class AuditedEntity
{
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Account : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Phone { get; set; } = "";
    public long GarageRevision { get; set; }
}

public sealed class UserSettings : AuditedEntity
{
    public Guid AccountId { get; set; }
    public bool Reminders { get; set; } = true;
    public decimal ThresholdKm { get; set; } = 500;
    public int ThresholdDays { get; set; } = 30;
}

public sealed class Vehicle : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string Name { get; set; } = "";
    public string Plate { get; set; } = "";
    public string PlateNormalized { get; set; } = "";
    public VehicleType Type { get; set; }
    public int Year { get; set; }
    public decimal BaseOdo { get; set; }
    public string ImageUrl { get; set; } = "";
    public DateTimeOffset? ArchivedAtUtc { get; set; }
}

public sealed class Entry : AuditedEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public Guid VehicleId { get; set; }
    public EntryKind Kind { get; set; }
    public string Title { get; set; } = "";
    public DateOnly Date { get; set; }
    public decimal Odo { get; set; }
    public decimal Cost { get; set; }
    public decimal Liters { get; set; }
    public string Note { get; set; } = "";
    public Guid? PlanId { get; set; }
}

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
