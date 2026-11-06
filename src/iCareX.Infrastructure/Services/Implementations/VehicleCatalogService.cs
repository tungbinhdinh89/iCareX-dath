using Microsoft.EntityFrameworkCore;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class VehicleCatalogService(GarageDbContext db) : IVehicleCatalogService
{
    public async Task<IReadOnlyCollection<VehicleTypeResponse>> ListTypesAsync(CancellationToken cancellationToken) =>
        await db.VehicleTypes.Where(x => x.IsActive).OrderBy(x => x.Name)
            .Select(x => new VehicleTypeResponse(x.Id, x.Code, x.Name, x.IsActive, x.Version))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<VehicleBrandResponse>> ListBrandsAsync(Guid vehicleTypeId, CancellationToken cancellationToken) =>
        await db.VehicleBrands.Where(x => x.VehicleTypeId == vehicleTypeId && x.IsActive && x.VehicleType!.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new VehicleBrandResponse(x.Id, x.VehicleTypeId, x.Name, x.IsActive, x.Version))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyCollection<VehicleModelResponse>> ListModelsAsync(Guid vehicleBrandId, CancellationToken cancellationToken) =>
        await db.VehicleModels.Where(x => x.VehicleBrandId == vehicleBrandId && x.IsActive && x.VehicleBrand!.IsActive &&
                x.VehicleBrand.VehicleType!.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new VehicleModelResponse(x.Id, x.VehicleBrandId, x.Name, x.IsActive, x.Version))
            .ToArrayAsync(cancellationToken);

    public async Task<VehicleTypeResponse> CreateTypeAsync(CreateVehicleTypeRequest request, CancellationToken cancellationToken)
    {
        var type = new VehicleType { Code = request.Code.Trim().ToUpperInvariant(), Name = request.Name.Trim() };
        db.VehicleTypes.Add(type);
        await db.SaveChangesAsync(cancellationToken);
        return new(type.Id, type.Code, type.Name, type.IsActive, type.Version);
    }

    public async Task<VehicleTypeResponse> UpdateTypeAsync(Guid id, UpdateVehicleTypeRequest request, CancellationToken cancellationToken)
    {
        var type = await db.VehicleTypes.FindAsync([id], cancellationToken) ?? throw new VehicleNotFoundException();
        if (type.Version != request.Version) throw new VehicleConflictException("Phiên bản loại xe không còn mới.");
        type.Code = request.Code.Trim().ToUpperInvariant();
        type.Name = request.Name.Trim();
        type.IsActive = request.IsActive;
        type.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return new(type.Id, type.Code, type.Name, type.IsActive, type.Version);
    }

    public async Task<VehicleBrandResponse> CreateBrandAsync(CreateVehicleBrandRequest request, CancellationToken cancellationToken)
    {
        if (!await db.VehicleTypes.AnyAsync(x => x.Id == request.VehicleTypeId && x.IsActive, cancellationToken))
            throw new VehicleValidationException(["Loại phương tiện không hợp lệ."]);
        var brand = new VehicleBrand { VehicleTypeId = request.VehicleTypeId, Name = request.Name.Trim() };
        db.VehicleBrands.Add(brand);
        await db.SaveChangesAsync(cancellationToken);
        return new(brand.Id, brand.VehicleTypeId, brand.Name, brand.IsActive, brand.Version);
    }

    public async Task<VehicleBrandResponse> UpdateBrandAsync(Guid id, UpdateVehicleBrandRequest request, CancellationToken cancellationToken)
    {
        var brand = await db.VehicleBrands.FindAsync([id], cancellationToken) ?? throw new VehicleNotFoundException();
        if (brand.Version != request.Version) throw new VehicleConflictException("Phiên bản hãng xe không còn mới.");
        if (!await db.VehicleTypes.AnyAsync(x => x.Id == request.VehicleTypeId && x.IsActive, cancellationToken))
            throw new VehicleValidationException(["Loại phương tiện không hợp lệ."]);
        brand.VehicleTypeId = request.VehicleTypeId;
        brand.Name = request.Name.Trim();
        brand.IsActive = request.IsActive;
        brand.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return new(brand.Id, brand.VehicleTypeId, brand.Name, brand.IsActive, brand.Version);
    }

    public async Task<VehicleModelResponse> CreateModelAsync(CreateVehicleModelRequest request, CancellationToken cancellationToken)
    {
        if (!await db.VehicleBrands.AnyAsync(x => x.Id == request.VehicleBrandId && x.IsActive && x.VehicleType!.IsActive, cancellationToken))
            throw new VehicleValidationException(["Hãng xe không hợp lệ."]);
        var model = new VehicleModel { VehicleBrandId = request.VehicleBrandId, Name = request.Name.Trim() };
        db.VehicleModels.Add(model);
        await db.SaveChangesAsync(cancellationToken);
        return new(model.Id, model.VehicleBrandId, model.Name, model.IsActive, model.Version);
    }

    public async Task<VehicleModelResponse> UpdateModelAsync(Guid id, UpdateVehicleModelRequest request, CancellationToken cancellationToken)
    {
        var model = await db.VehicleModels.FindAsync([id], cancellationToken) ?? throw new VehicleNotFoundException();
        if (model.Version != request.Version) throw new VehicleConflictException("Phiên bản dòng xe không còn mới.");
        if (!await db.VehicleBrands.AnyAsync(x => x.Id == request.VehicleBrandId && x.IsActive && x.VehicleType!.IsActive, cancellationToken))
            throw new VehicleValidationException(["Hãng xe không hợp lệ."]);
        model.VehicleBrandId = request.VehicleBrandId;
        model.Name = request.Name.Trim();
        model.IsActive = request.IsActive;
        model.Version++;
        await db.SaveChangesAsync(cancellationToken);
        return new(model.Id, model.VehicleBrandId, model.Name, model.IsActive, model.Version);
    }
}
