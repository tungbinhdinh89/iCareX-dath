using System.Text.Json.Serialization;

namespace iCareX.Shared.Contracts.Vehicles;

public sealed record VehicleResponse(
    Guid Id, string? Name, string? Plate, string VehicleType, string VehicleTypeName,
    Guid VehicleBrandId, string VehicleBrandName, Guid VehicleModelId, string VehicleModelName,
    int? Year, decimal Odometer, decimal CurrentOdo, string FuelType, decimal? FuelCapacity,
    string OdometerUnit, string? Vin, string? RegistrationId,
    decimal? PurchasePrice, decimal? SalePrice, string? Notes,
    string? ImageUrl, bool IsArchived, long Version, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);

public sealed record VehicleListResponse(IReadOnlyCollection<VehicleResponse> Vehicles);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record CreateVehicleRequest(
    string VehicleType, Guid VehicleBrandId, Guid VehicleModelId, string FuelType,
    string OdometerUnit, decimal Odometer,
    int? Year, string? Name, string? Plate, string? Vin, string? RegistrationId,
    decimal? FuelCapacity, decimal? PurchasePrice, decimal? SalePrice,
    string? Notes, string? ImageUrl);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateVehicleRequest(
    string VehicleType, Guid VehicleBrandId, Guid VehicleModelId, string FuelType,
    string OdometerUnit, decimal Odometer,
    int? Year, string? Name, string? Plate, string? Vin, string? RegistrationId,
    decimal? FuelCapacity, decimal? PurchasePrice, decimal? SalePrice,
    string? Notes, string? ImageUrl, long Version);

public sealed record OdometerReadingResponse(
    Guid Id,
    Guid VehicleId,
    DateOnly Date,
    decimal Odo,
    string Title,
    string Note,
    long Version);

public sealed record CreateOdometerReadingRequest(
    DateOnly Date,
    decimal Odo,
    string? Note);

public sealed record UpdateOdometerReadingRequest(
    DateOnly Date,
    decimal Odo,
    string? Note,
    long Version);

public sealed record VehicleTypeResponse(string Id, string Code, string Name);
public sealed record VehicleBrandResponse(Guid Id, string VehicleType, string Name, bool IsActive, long Version);
public sealed record VehicleModelResponse(Guid Id, Guid VehicleBrandId, string Name, bool IsActive, long Version);
public sealed record CreateVehicleBrandRequest(string VehicleType, string Name);
public sealed record UpdateVehicleBrandRequest(string VehicleType, string Name, bool IsActive, long Version);
public sealed record CreateVehicleModelRequest(Guid VehicleBrandId, string Name);
public sealed record UpdateVehicleModelRequest(Guid VehicleBrandId, string Name, bool IsActive, long Version);
