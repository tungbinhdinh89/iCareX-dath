namespace iCareX.Shared.Contracts.Vehicles;

public sealed record VehicleResponse(
    Guid Id,
    string Name,
    string Plate,
    Guid VehicleTypeId,
    string VehicleTypeName,
    Guid VehicleBrandId,
    string VehicleBrandName,
    Guid VehicleModelId,
    string VehicleModelName,
    int Year,
    decimal BaseOdo,
    decimal CurrentOdo,
    string ImageUrl,
    bool IsArchived,
    long Version);

public sealed record VehicleListResponse(IReadOnlyCollection<VehicleResponse> Vehicles);

public sealed record CreateVehicleRequest(
    string Name,
    string Plate,
    Guid VehicleModelId,
    int Year,
    decimal BaseOdo,
    string? ImageUrl);

public sealed record UpdateVehicleRequest(
    string Name,
    string Plate,
    Guid VehicleModelId,
    int Year,
    decimal BaseOdo,
    string? ImageUrl,
    long Version);

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

public sealed record VehicleTypeResponse(Guid Id, string Code, string Name, bool IsActive, long Version);

public sealed record VehicleBrandResponse(Guid Id, Guid VehicleTypeId, string Name, bool IsActive, long Version);

public sealed record VehicleModelResponse(Guid Id, Guid VehicleBrandId, string Name, bool IsActive, long Version);

public sealed record CreateVehicleTypeRequest(string Code, string Name);

public sealed record UpdateVehicleTypeRequest(string Code, string Name, bool IsActive, long Version);

public sealed record CreateVehicleBrandRequest(Guid VehicleTypeId, string Name);

public sealed record UpdateVehicleBrandRequest(Guid VehicleTypeId, string Name, bool IsActive, long Version);

public sealed record CreateVehicleModelRequest(Guid VehicleBrandId, string Name);

public sealed record UpdateVehicleModelRequest(Guid VehicleBrandId, string Name, bool IsActive, long Version);
