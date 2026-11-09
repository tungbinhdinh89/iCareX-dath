using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using iCareX.Domain.Entities;

namespace iCareX.Infrastructure.Persistence;

public sealed class GarageDbContext(DbContextOptions<GarageDbContext> options)
    : IdentityDbContext<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<UserSettings> Settings => Set<UserSettings>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<OdometerReading> OdometerReadings => Set<OdometerReading>();
    public DbSet<FuelEntry> FuelEntries => Set<FuelEntry>();
    public DbSet<MaintenanceRecord> MaintenanceRecords => Set<MaintenanceRecord>();
    public DbSet<VehicleMember> VehicleMembers => Set<VehicleMember>();
    public DbSet<VehicleInvitation> VehicleInvitations => Set<VehicleInvitation>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<MaintenancePlan> Plans => Set<MaintenancePlan>();
    public DbSet<AuthSession> Sessions => Set<AuthSession>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeCatalogNames();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeCatalogNames();
        return base.SaveChanges();
    }

    private void NormalizeCatalogNames()
    {
        foreach (var entry in ChangeTracker.Entries<VehicleBrand>().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.Name = entry.Entity.Name.Trim();
            entry.Entity.NormalizedName = iCareX.Domain.Common.VehicleRegistrationRules.NormalizeName(entry.Entity.Name);
        }
        foreach (var entry in ChangeTracker.Entries<VehicleModel>().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            entry.Entity.Name = entry.Entity.Name.Trim();
            entry.Entity.NormalizedName = iCareX.Domain.Common.VehicleRegistrationRules.NormalizeName(entry.Entity.Name);
        }
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<Account>(e =>
        {
            e.ToTable("accounts", t => t.HasCheckConstraint("ck_account_revision", "\"GarageRevision\" >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.Phone).HasMaxLength(10);
            e.Property(x => x.GarageRevision).IsConcurrencyToken();
            e.HasOne<ApplicationUser>().WithOne().HasForeignKey<Account>(x => x.Id).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ApplicationUser>().HasIndex(x => x.NormalizedEmail).IsUnique();
        b.Entity<UserSettings>(e =>
        {
            e.ToTable("user_settings", t => t.HasCheckConstraint("ck_settings_thresholds",
                "\"ThresholdKm\" BETWEEN 1 AND 100000 AND \"ThresholdDays\" BETWEEN 1 AND 365"));
            e.HasKey(x => x.AccountId);
            e.HasOne<Account>().WithOne().HasForeignKey<UserSettings>(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
        b.ApplyConfiguration(new Configurations.VehicleConfiguration());
        b.ApplyConfiguration(new Configurations.VehicleBrandConfiguration());
        b.ApplyConfiguration(new Configurations.VehicleModelConfiguration());
        b.Entity<MaintenancePlan>(e =>
        {
            e.ToTable("maintenance_plans", t => t.HasCheckConstraint("ck_plan_intervals",
                "\"DueOdo\" BETWEEN 1 AND 9999999999.99 AND \"IntervalKm\" BETWEEN 1 AND 1000000 AND \"IntervalMonths\" BETWEEN 1 AND 120"));
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.AccountId, x.VehicleId, x.Id });
            e.Property(x => x.Title).HasMaxLength(100);
            e.HasIndex(x => new { x.AccountId, x.VehicleId });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId })
                .HasPrincipalKey(x => new { x.AccountId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<OdometerReading>(e =>
        {
            e.ToTable("odometer_readings", t => t.HasCheckConstraint("ck_odometer_reading_odo", "\"Odo\" BETWEEN 0 AND 9999999999.99"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.AccountId, x.VehicleId, x.Date, x.Odo });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId })
                .HasPrincipalKey(x => new { x.AccountId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<FuelEntry>(e =>
        {
            e.ToTable("fuel_entries", t =>
            {
                t.HasCheckConstraint("ck_fuel_entry_numbers", "\"Odo\" BETWEEN 0 AND 9999999999.99 AND \"Cost\" > 0 AND \"Liters\" > 0 AND \"Liters\" <= 500");
            });
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AccountId, x.VehicleId, x.Date, x.Odo });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId })
                .HasPrincipalKey(x => new { x.AccountId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MaintenanceRecord>(e =>
        {
            e.ToTable("maintenance_records", t => t.HasCheckConstraint("ck_maintenance_record_numbers",
                "\"Odo\" BETWEEN 0 AND 9999999999.99 AND \"Cost\" > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.AccountId, x.VehicleId, x.Date, x.Odo });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId })
                .HasPrincipalKey(x => new { x.AccountId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DriverUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<MaintenancePlan>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId, x.PlanId })
                .HasPrincipalKey(x => new { x.AccountId, x.VehicleId, x.Id }).OnDelete(DeleteBehavior.NoAction);
        });
        b.Entity<VehicleMember>(e =>
        {
            e.ToTable("vehicle_members", t => t.HasCheckConstraint("ck_vehicle_member_role",
                "\"Role\" IN ('Owner', 'Manager', 'Contributor', 'Viewer')"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => new { x.VehicleId, x.UserId }).HasFilter("\"RevokedAtUtc\" IS NULL").IsUnique();
            e.HasIndex(x => x.VehicleId).HasFilter("\"RevokedAtUtc\" IS NULL AND \"Role\" = 'Owner'").IsUnique();
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<VehicleInvitation>(e =>
        {
            e.ToTable("vehicle_invitations", t =>
            {
                t.HasCheckConstraint("ck_vehicle_invitation_role",
                    "\"Role\" IN ('Manager', 'Contributor', 'Viewer')");
                t.HasCheckConstraint("ck_vehicle_invitation_status",
                    "\"Status\" IN ('Pending', 'Accepted', 'Rejected', 'Revoked', 'Expired')");
                t.HasCheckConstraint("ck_vehicle_invitation_acceptance",
                    "(\"Status\" = 'Accepted' AND \"AcceptedByUserId\" IS NOT NULL AND \"AcceptedAtUtc\" IS NOT NULL) OR \"Status\" <> 'Accepted'");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.InvitedEmail).HasMaxLength(320);
            e.Property(x => x.NormalizedInvitedEmail).HasMaxLength(320);
            e.Property(x => x.TokenHash).HasMaxLength(128);
            e.Property(x => x.Role).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => x.TokenHash).IsUnique();
            e.HasIndex(x => new { x.VehicleId, x.NormalizedInvitedEmail })
                .HasFilter("\"Status\" = 'Pending'").IsUnique();
            e.HasIndex(x => new { x.VehicleId, x.Status, x.ExpiresAtUtc });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.InvitedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AcceptedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Reminder>(e =>
        {
            e.ToTable("reminders", t =>
            {
                t.HasCheckConstraint("ck_reminder_due_odo",
                    "\"DueOdo\" IS NULL OR \"DueOdo\" BETWEEN 0 AND 9999999");
                t.HasCheckConstraint("ck_reminder_status",
                    "\"Status\" IN ('Pending', 'Completed', 'Cancelled')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Title).HasMaxLength(100);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => new { x.VehicleId, x.Status, x.DueAtUtc, x.DueOdo });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CompletedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReferenceType).HasMaxLength(64);
            e.Property(x => x.Snapshot).HasMaxLength(4000);
            e.Property(x => x.DeduplicationKey).HasMaxLength(256);
            e.HasIndex(x => new { x.RecipientUserId, x.ReadAtUtc, x.CreatedAtUtc, x.Id });
            e.HasIndex(x => new { x.RecipientUserId, x.DeduplicationKey }).IsUnique();
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Action).HasMaxLength(100);
            e.Property(x => x.EntityType).HasMaxLength(100);
            e.Property(x => x.OldValues).HasColumnType("jsonb");
            e.Property(x => x.NewValues).HasColumnType("jsonb");
            e.HasIndex(x => new { x.VehicleId, x.CreatedAtUtc });
            e.HasIndex(x => new { x.ActorUserId, x.CreatedAtUtc });
            e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => x.VehicleId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<AuthSession>(e =>
        {
            e.ToTable("auth_sessions");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.AccountId, x.ExpiresAtUtc });
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<IdempotencyRecord>(e =>
        {
            e.ToTable("idempotency_records");
            e.HasKey(x => new { x.AccountId, x.Key });
            e.Property(x => x.Key).HasMaxLength(128);
            e.Property(x => x.RequestHash).HasMaxLength(64);
            e.Property(x => x.ResponseETag).HasMaxLength(64);
            e.HasIndex(x => x.ExpiresAtUtc);
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
        foreach (var entity in b.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
            {
                if (property.GetPrecision() is null) property.SetPrecision(20);
                if (property.GetScale() is null) property.SetScale(6);
            }
        }
    }
}
