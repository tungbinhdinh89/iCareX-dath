using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Application.Features.Vehicles.Contracts;

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

public interface IVehicleCatalogService
{
    Task<IReadOnlyCollection<VehicleTypeResponse>> ListTypesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VehicleBrandResponse>> ListBrandsAsync(Guid vehicleTypeId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<VehicleModelResponse>> ListModelsAsync(Guid vehicleBrandId, CancellationToken cancellationToken);
    Task<VehicleTypeResponse> CreateTypeAsync(CreateVehicleTypeRequest request, CancellationToken cancellationToken);
    Task<VehicleTypeResponse> UpdateTypeAsync(Guid id, UpdateVehicleTypeRequest request, CancellationToken cancellationToken);
    Task<VehicleBrandResponse> CreateBrandAsync(CreateVehicleBrandRequest request, CancellationToken cancellationToken);
    Task<VehicleBrandResponse> UpdateBrandAsync(Guid id, UpdateVehicleBrandRequest request, CancellationToken cancellationToken);
    Task<VehicleModelResponse> CreateModelAsync(CreateVehicleModelRequest request, CancellationToken cancellationToken);
    Task<VehicleModelResponse> UpdateModelAsync(Guid id, UpdateVehicleModelRequest request, CancellationToken cancellationToken);
}

public sealed class VehicleNotFoundException : Exception;
public sealed class VehicleConflictException(string message) : Exception(message);
public sealed class VehicleValidationException(IReadOnlyCollection<string> errors) : Exception
{
    public IReadOnlyCollection<string> Errors { get; } = errors;
}
