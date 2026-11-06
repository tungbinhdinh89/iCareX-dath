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
    public DbSet<VehicleType> VehicleTypes => Set<VehicleType>();
    public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<Entry> Entries => Set<Entry>();
    public DbSet<MaintenancePlan> Plans => Set<MaintenancePlan>();
    public DbSet<AuthSession> Sessions => Set<AuthSession>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

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
        b.Entity<Vehicle>(e =>
        {
            e.ToTable("vehicles", t =>
            {
                t.HasCheckConstraint("ck_vehicle_odo", "\"BaseOdo\" BETWEEN 0 AND 9999999");
                t.HasCheckConstraint("ck_vehicle_year", "\"Year\" >= 1980");
            });
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.AccountId, x.Id });
            e.HasIndex(x => new { x.AccountId, x.PlateNormalized }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.Plate).HasMaxLength(20);
            e.Property(x => x.PlateNormalized).HasMaxLength(20);
            e.Property(x => x.ImageUrl).HasMaxLength(2048);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.VehicleModel).WithMany().HasForeignKey(x => x.VehicleModelId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<VehicleType>(e =>
        {
            e.ToTable("vehicle_types");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(32);
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.Version).IsConcurrencyToken();
        });
        b.Entity<VehicleBrand>(e =>
        {
            e.ToTable("vehicle_brands");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.VehicleTypeId, x.Name }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.VehicleType).WithMany(x => x.Brands).HasForeignKey(x => x.VehicleTypeId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<VehicleModel>(e =>
        {
            e.ToTable("vehicle_models");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.VehicleBrandId, x.Name }).IsUnique();
            e.Property(x => x.Name).HasMaxLength(60);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.HasOne(x => x.VehicleBrand).WithMany(x => x.Models).HasForeignKey(x => x.VehicleBrandId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<MaintenancePlan>(e =>
        {
            e.ToTable("maintenance_plans", t => t.HasCheckConstraint("ck_plan_intervals",
                "\"DueOdo\" BETWEEN 1 AND 9999999 AND \"IntervalKm\" BETWEEN 1 AND 1000000 AND \"IntervalMonths\" BETWEEN 1 AND 120"));
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.AccountId, x.VehicleId, x.Id });
            e.Property(x => x.Title).HasMaxLength(100);
            e.HasIndex(x => new { x.AccountId, x.VehicleId });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId })
                .HasPrincipalKey(x => new { x.AccountId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Entry>(e =>
        {
            e.ToTable("entries", t =>
            {
                t.HasCheckConstraint("ck_entry_numbers", "\"Odo\" BETWEEN 0 AND 9999999 AND \"Cost\" BETWEEN 0 AND 10000000000 AND \"Liters\" BETWEEN 0 AND 500");
                t.HasCheckConstraint("ck_entry_kind", "(\"Kind\" = 'Fuel' AND \"Cost\" > 0 AND \"Liters\" > 0 AND \"PlanId\" IS NULL) OR (\"Kind\" = 'Service' AND \"Cost\" > 0 AND \"Liters\" = 0) OR (\"Kind\" = 'Odo' AND \"Cost\" = 0 AND \"Liters\" = 0 AND \"PlanId\" IS NULL)");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(16);
            e.Property(x => x.Version).IsConcurrencyToken();
            e.Property(x => x.Title).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(500);
            e.HasIndex(x => new { x.AccountId, x.VehicleId, x.Date, x.Odo });
            e.HasIndex(x => new { x.AccountId, x.PlanId, x.Date, x.Odo });
            e.HasOne<Vehicle>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId })
                .HasPrincipalKey(x => new { x.AccountId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            // Unlink entries explicitly before deleting a plan; SET NULL on this composite FK would also null owner/vehicle.
            e.HasOne<MaintenancePlan>().WithMany().HasForeignKey(x => new { x.AccountId, x.VehicleId, x.PlanId })
                .HasPrincipalKey(x => new { x.AccountId, x.VehicleId, x.Id }).OnDelete(DeleteBehavior.NoAction);
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
                property.SetPrecision(20);
                property.SetScale(6);
            }
        }
    }
}
