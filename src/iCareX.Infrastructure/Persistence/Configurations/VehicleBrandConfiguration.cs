using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iCareX.Domain.Entities;

namespace iCareX.Infrastructure.Persistence.Configurations;

public sealed class VehicleBrandConfiguration : IEntityTypeConfiguration<VehicleBrand>
{
    public void Configure(EntityTypeBuilder<VehicleBrand> e)
    {
        e.ToTable("vehicle_brands", t =>
        {
            t.HasCheckConstraint("ck_vehicle_brand_type", "\"VehicleType\" IN ('Car','Motorcycle','Bicycle','Truck','Bus','Van','Boat','Aircraft','Other')");
            t.HasCheckConstraint("ck_vehicle_brand_name", "length(btrim(\"Name\")) > 0 AND length(\"NormalizedName\") > 0");
        });
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.Id, x.VehicleType });
        e.Property(x => x.VehicleType).HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.NormalizedName).HasMaxLength(150).IsRequired();
        e.HasIndex(x => new { x.VehicleType, x.NormalizedName }).IsUnique();
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.Version).IsConcurrencyToken();
    }
}
