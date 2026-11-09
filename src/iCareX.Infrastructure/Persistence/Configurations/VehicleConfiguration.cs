using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iCareX.Domain.Entities;

namespace iCareX.Infrastructure.Persistence.Configurations;

public sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> e)
    {
        e.ToTable("vehicles", t =>
        {
            t.HasCheckConstraint("ck_vehicle_odo", "\"Odometer\" BETWEEN 0 AND 9999999999.99");
            t.HasCheckConstraint("ck_vehicle_year", "\"Year\" IS NULL OR \"Year\" BETWEEN 1 AND 9999");
            t.HasCheckConstraint("ck_vehicle_type", "\"VehicleType\" IN ('Car','Motorcycle','Bicycle','Truck','Bus','Van','Boat','Aircraft','Other')");
            t.HasCheckConstraint("ck_vehicle_fuel_type", "\"FuelType\" IN ('Gasoline','Diesel','Electric','Hybrid','PlugInHybrid','Ethanol','Biodiesel','LPG','CNG','Hydrogen','Other')");
            t.HasCheckConstraint("ck_vehicle_odometer_unit", "\"OdometerUnit\" IN ('Kilometres','Miles','Hours')");
            t.HasCheckConstraint("ck_vehicle_optional_numbers", "(\"FuelCapacity\" IS NULL OR \"FuelCapacity\" > 0) AND (\"PurchasePrice\" IS NULL OR \"PurchasePrice\" >= 0) AND (\"SalePrice\" IS NULL OR \"SalePrice\" >= 0)");
            t.HasCheckConstraint("ck_vehicle_plate_pair", "(\"Plate\" IS NULL AND \"PlateNormalized\" IS NULL) OR (\"Plate\" IS NOT NULL AND \"PlateNormalized\" IS NOT NULL AND length(\"PlateNormalized\") > 0)");
        });
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.AccountId, x.Id });
        e.HasIndex(x => new { x.AccountId, x.PlateNormalized }).HasFilter("\"PlateNormalized\" IS NOT NULL").IsUnique();
        e.HasIndex(x => x.CreatedByUserId);
        e.HasIndex(x => new { x.VehicleBrandId, x.VehicleModelId });
        e.HasIndex(x => x.PlateNormalized);
        e.HasIndex(x => x.VIN);
        e.HasIndex(x => x.ArchivedAtUtc);
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.Plate).HasMaxLength(30);
        e.Property(x => x.PlateNormalized).HasMaxLength(30);
        e.Property(x => x.VIN).HasMaxLength(50);
        e.Property(x => x.RegistrationId).HasMaxLength(100);
        e.Property(x => x.Notes).HasMaxLength(2000);
        e.Property(x => x.ImageUrl).HasMaxLength(2048);
        e.Property(x => x.VehicleType).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.FuelType).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.OdometerUnit).HasConversion<string>().HasMaxLength(16);
        e.Property(x => x.Odometer).HasPrecision(12, 2).HasDefaultValue(0m);
        e.Property(x => x.FuelCapacity).HasPrecision(10, 2);
        e.Property(x => x.PurchasePrice).HasPrecision(18, 2);
        e.Property(x => x.SalePrice).HasPrecision(18, 2);
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        e.HasOne<Account>().WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.VehicleBrand).WithMany(x => x.Vehicles)
            .HasForeignKey(x => new { x.VehicleBrandId, x.VehicleType })
            .HasPrincipalKey(x => new { x.Id, x.VehicleType }).OnDelete(DeleteBehavior.Restrict);
        e.HasOne(x => x.VehicleModel).WithMany(x => x.Vehicles)
            .HasForeignKey(x => new { x.VehicleModelId, x.VehicleBrandId })
            .HasPrincipalKey(x => new { x.Id, x.VehicleBrandId }).OnDelete(DeleteBehavior.Restrict);
    }
}
