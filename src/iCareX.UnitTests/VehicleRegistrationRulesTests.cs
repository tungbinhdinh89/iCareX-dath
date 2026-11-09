using iCareX.Domain.Common;
using iCareX.Domain.Entities;
using iCareX.Domain.Enums;
using iCareX.Domain.Exceptions;
using Xunit;

namespace iCareX.UnitTests;

public sealed class VehicleRegistrationRulesTests
{
    private static Vehicle Valid() => new()
    {
        VehicleType = VehicleType.Car, VehicleBrandId = Guid.NewGuid(), VehicleModelId = Guid.NewGuid(),
        FuelType = FuelType.Electric, OdometerUnit = OdometerUnit.Kilometres
    };

    [Fact]
    public void MinimalRegistrationAllowsAllOptionalFieldsAbsent()
    {
        var vehicle = Valid();
        VehicleRegistrationRules.Validate(vehicle, 2026);
        Assert.Null(vehicle.Name); Assert.Null(vehicle.Plate); Assert.Null(vehicle.Year);
        Assert.Equal(0, vehicle.Odometer);
    }

    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(50)]
    public void UndefinedEnumsAreRejected(int value)
    {
        var vehicle = Valid();
        vehicle.VehicleType = (VehicleType)value;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.FuelType = (FuelType)value;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.OdometerUnit = (OdometerUnit)value;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
    }

    [Theory]
    [InlineData(-1)] [InlineData(10000000000)]
    public void NegativeOrOverflowOdometerIsRejected(decimal value)
    {
        var vehicle = Valid(); vehicle.Odometer = value;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
    }

    [Fact]
    public void NumericPrecisionAndOptionalNumbersAreValidated()
    {
        var vehicle = Valid(); vehicle.Odometer = 0.001m;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.FuelCapacity = 0;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.PurchasePrice = -1;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.SalePrice = -1;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.PurchasePrice = 0; vehicle.SalePrice = 0;
        vehicle.FuelCapacity = 12.34m; vehicle.Odometer = VehicleRegistrationRules.MaximumOdometer;
        VehicleRegistrationRules.Validate(vehicle, 2026);
    }

    [Fact]
    public void OptionalTextLimitsAndYearAreEnforced()
    {
        var vehicle = Valid(); vehicle.Name = new string('x', 151);
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.Plate = new string('x', 31);
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.Notes = new string('x', 2001);
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle = Valid(); vehicle.Year = 2028;
        Assert.Throws<RuleViolation>(() => VehicleRegistrationRules.Validate(vehicle, 2026));
        vehicle.Year = 2027; VehicleRegistrationRules.Validate(vehicle, 2026);
    }

    [Fact]
    public void IdentifiersAndCatalogNamesNormalizeWithoutInventingValues()
    {
        Assert.Null(VehicleRegistrationRules.NormalizePlate("  "));
        Assert.Null(VehicleRegistrationRules.NormalizeVin(null));
        Assert.Equal("51G87624", VehicleRegistrationRules.NormalizePlate(" 51g-876.24 "));
        Assert.Equal("ABC123", VehicleRegistrationRules.NormalizeVin(" abc 123 "));
        Assert.Equal(VehicleRegistrationRules.NormalizeName("  Honda    Civic "), VehicleRegistrationRules.NormalizeName("honda civic"));
    }
}
