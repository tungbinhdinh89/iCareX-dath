using iCareX.Domain.Entities;
using iCareX.Domain.Enums;

namespace iCareX.IntegrationTests;

public sealed partial class DatabaseTests
{
    private static VehicleModel TestVehicleModel()
    {
        var brand = new VehicleBrand { Name = $"Brand {Guid.NewGuid():N}", VehicleType = VehicleType.Car };
        return new VehicleModel { Name = "Model", VehicleBrandId = brand.Id, VehicleBrand = brand };
    }
}
