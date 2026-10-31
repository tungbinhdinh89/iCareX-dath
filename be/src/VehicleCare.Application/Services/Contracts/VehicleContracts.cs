using VehicleCare.Domain;

namespace VehicleCare.Application.Services.Contracts;

public sealed record VehicleResponse(
    Guid Id,
    string Name,
    string Plate,
    string Type,
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
    VehicleType Type,
    int Year,
    decimal BaseOdo,
    string? ImageUrl);

public sealed record UpdateVehicleRequest(
    string Name,
    string Plate,
    VehicleType Type,
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

public interface IVehicleService
{
    Task<IReadOnlyCollection<VehicleResponse>> ListAsync(Guid accountId, bool includeArchived, CancellationToken cancellationToken);
    Task<VehicleResponse?> GetAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken);
    Task<VehicleResponse> CreateAsync(Guid accountId, CreateVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleResponse> UpdateAsync(Guid accountId, Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken);
    Task<VehicleResponse> ArchiveAsync(Guid accountId, Guid vehicleId, long version, CancellationToken cancellationToken);
    Task<VehicleResponse> RestoreAsync(Guid accountId, Guid vehicleId, long version, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<OdometerReadingResponse>?> ListReadingsAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken);
    Task<OdometerReadingResponse> CreateReadingAsync(Guid accountId, Guid vehicleId, CreateOdometerReadingRequest request, CancellationToken cancellationToken);
    Task<OdometerReadingResponse> UpdateReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, UpdateOdometerReadingRequest request, CancellationToken cancellationToken);
    Task DeleteReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, long version, CancellationToken cancellationToken);
}

public sealed class VehicleNotFoundException : Exception;
public sealed class VehicleConflictException(string message) : Exception(message);
public sealed class VehicleValidationException(IReadOnlyCollection<string> errors) : Exception
{
    public IReadOnlyCollection<string> Errors { get; } = errors;
}
