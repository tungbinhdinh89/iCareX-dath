using Microsoft.EntityFrameworkCore;
using iCareX.Domain.Entities;
using iCareX.Domain.Enums;
using iCareX.Infrastructure.Persistence;
using Xunit;

namespace iCareX.IntegrationTests;

[Trait("Category", "Host")]
public sealed class VehicleSchemaModelTests
{
    [Fact]
    public void RelationalModelHasUnambiguousCatalogCompositeKeysAndOptionalFields()
    {
        using var db = new GarageDbContext(new DbContextOptionsBuilder<GarageDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=test;Password=test").Options);
        var model = db.Model;
        Assert.Null(model.FindEntityType("iCareX.Domain.Entities.VehicleType"));
        var vehicle = model.FindEntityType(typeof(Vehicle))!;
        Assert.Null(vehicle.FindProperty("HasTwoTanks"));
        Assert.Null(vehicle.FindProperty("SpeedLimit"));
        foreach (var name in new[] { "Name", "Plate", "PlateNormalized", "Year", "VIN", "RegistrationId", "FuelCapacity", "PurchasePrice", "SalePrice", "Notes", "ImageUrl" })
            Assert.True(vehicle.FindProperty(name)!.IsNullable, name);
        foreach (var name in new[] { "VehicleType", "VehicleBrandId", "VehicleModelId", "FuelType", "Odometer", "OdometerUnit" })
            Assert.False(vehicle.FindProperty(name)!.IsNullable, name);
        var modelFk = Assert.Single(vehicle.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(VehicleModel));
        Assert.Equal(new[] { "VehicleModelId", "VehicleBrandId" }, modelFk.Properties.Select(x => x.Name));
        Assert.Equal(DeleteBehavior.Restrict, modelFk.DeleteBehavior);
        var brandFk = Assert.Single(vehicle.GetForeignKeys(), x => x.PrincipalEntityType.ClrType == typeof(VehicleBrand));
        Assert.Equal(new[] { "VehicleBrandId", "VehicleType" }, brandFk.Properties.Select(x => x.Name));
        Assert.Equal(DeleteBehavior.Restrict, brandFk.DeleteBehavior);
        Assert.Equal(12, vehicle.FindProperty("Odometer")!.GetPrecision());
        Assert.Equal(2, vehicle.FindProperty("Odometer")!.GetScale());
        Assert.Equal(18, vehicle.FindProperty("PurchasePrice")!.GetPrecision());
        Assert.Equal("Car", vehicle.FindProperty("VehicleType")!.GetTypeMapping().Converter!.ConvertToProvider(VehicleType.Car));
    }
}
