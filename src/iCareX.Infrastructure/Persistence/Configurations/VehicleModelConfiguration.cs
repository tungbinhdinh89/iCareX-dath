using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using iCareX.Domain.Entities;

namespace iCareX.Infrastructure.Persistence.Configurations;

public sealed class VehicleModelConfiguration : IEntityTypeConfiguration<VehicleModel>
{
    public void Configure(EntityTypeBuilder<VehicleModel> e)
    {
        e.ToTable("vehicle_models", t => t.HasCheckConstraint("ck_vehicle_model_name", "length(btrim(\"Name\")) > 0 AND length(\"NormalizedName\") > 0"));
        e.HasKey(x => x.Id);
        e.HasAlternateKey(x => new { x.Id, x.VehicleBrandId });
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.NormalizedName).HasMaxLength(150).IsRequired();
        e.HasIndex(x => new { x.VehicleBrandId, x.NormalizedName }).IsUnique();
        e.Property(x => x.IsActive).HasDefaultValue(true);
        e.Property(x => x.Version).IsConcurrencyToken();
        e.HasOne(x => x.VehicleBrand).WithMany(x => x.Models).HasForeignKey(x => x.VehicleBrandId).OnDelete(DeleteBehavior.Restrict);
    }
}
