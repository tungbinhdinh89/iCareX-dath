using Microsoft.EntityFrameworkCore;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Domain.Common;
using iCareX.Domain.Entities;
using iCareX.Domain.Enums;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class VehicleCatalogService(GarageDbContext db) : IVehicleCatalogService
{
    public Task<IReadOnlyCollection<VehicleTypeResponse>> ListTypesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<VehicleTypeResponse>>(Enum.GetValues<VehicleType>()
            .Select(x => new VehicleTypeResponse(x.ToString(), x.ToString(), TypeLabel(x))).ToArray());

    public async Task<IReadOnlyCollection<VehicleBrandResponse>> ListBrandsAsync(string vehicleType, CancellationToken cancellationToken)
    {
        var type = ParseEnum<VehicleType>(vehicleType);
        return await db.VehicleBrands.AsNoTracking().Where(x => x.VehicleType == type && x.IsActive)
            .OrderBy(x => x.Name).Select(x => new VehicleBrandResponse(x.Id, x.VehicleType.ToString(), x.Name, x.IsActive, x.Version))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<VehicleModelResponse>> ListModelsAsync(Guid vehicleBrandId, CancellationToken cancellationToken) =>
        await db.VehicleModels.AsNoTracking().Where(x => x.VehicleBrandId == vehicleBrandId && x.IsActive && x.VehicleBrand!.IsActive)
            .OrderBy(x => x.Name).Select(x => new VehicleModelResponse(x.Id, x.VehicleBrandId, x.Name, x.IsActive, x.Version))
            .ToArrayAsync(cancellationToken);

    public async Task<VehicleBrandResponse> CreateBrandAsync(CreateVehicleBrandRequest request, CancellationToken ct)
    {
        var type = ParseEnum<VehicleType>(request.VehicleType);
        var name = Name(request.Name);
        var normalized = VehicleRegistrationRules.NormalizeName(name);
        if (await db.VehicleBrands.AnyAsync(x => x.VehicleType == type && x.NormalizedName == normalized, ct))
            throw new VehicleConflictException("Tên hãng đã tồn tại trong loại phương tiện này.");
        var brand = new VehicleBrand { VehicleType = type, Name = name, NormalizedName = normalized };
        db.VehicleBrands.Add(brand);
        await Save(ct);
        return new(brand.Id, brand.VehicleType.ToString(), brand.Name, brand.IsActive, brand.Version);
    }

    public async Task<VehicleBrandResponse> UpdateBrandAsync(Guid id, UpdateVehicleBrandRequest request, CancellationToken ct)
    {
        var brand = await db.VehicleBrands.FindAsync([id], ct) ?? throw new VehicleNotFoundException();
        if (brand.Version != request.Version) throw new VehicleConflictException("Phiên bản hãng xe đã thay đổi.");
        if (brand.VehicleType != ParseEnum<VehicleType>(request.VehicleType))
            throw new VehicleConflictException("Không thể chuyển hãng sang loại xe khác; hãy tạo hãng mới và ngừng sử dụng hãng cũ.");
        var name = Name(request.Name);
        var normalized = VehicleRegistrationRules.NormalizeName(name);
        if (await db.VehicleBrands.AnyAsync(x => x.Id != id && x.VehicleType == brand.VehicleType && x.NormalizedName == normalized, ct))
            throw new VehicleConflictException("Tên hãng đã tồn tại trong loại phương tiện này.");
        brand.Name = name; brand.NormalizedName = normalized; brand.IsActive = request.IsActive; brand.Version++;
        await Save(ct);
        return new(brand.Id, brand.VehicleType.ToString(), brand.Name, brand.IsActive, brand.Version);
    }

    public async Task<VehicleModelResponse> CreateModelAsync(CreateVehicleModelRequest request, CancellationToken ct)
    {
        if (!await db.VehicleBrands.AnyAsync(x => x.Id == request.VehicleBrandId && x.IsActive, ct))
            throw new VehicleValidationException(["Vui lòng chọn hãng xe đang hoạt động."]);
        var name = Name(request.Name);
        var normalized = VehicleRegistrationRules.NormalizeName(name);
        if (await db.VehicleModels.AnyAsync(x => x.VehicleBrandId == request.VehicleBrandId && x.NormalizedName == normalized, ct))
            throw new VehicleConflictException("Tên dòng xe đã tồn tại trong hãng này.");
        var model = new VehicleModel { VehicleBrandId = request.VehicleBrandId, Name = name, NormalizedName = normalized };
        db.VehicleModels.Add(model);
        await Save(ct);
        return new(model.Id, model.VehicleBrandId, model.Name, model.IsActive, model.Version);
    }

    public async Task<VehicleModelResponse> UpdateModelAsync(Guid id, UpdateVehicleModelRequest request, CancellationToken ct)
    {
        var model = await db.VehicleModels.FindAsync([id], ct) ?? throw new VehicleNotFoundException();
        if (model.Version != request.Version) throw new VehicleConflictException("Phiên bản dòng xe đã thay đổi.");
        if (model.VehicleBrandId != request.VehicleBrandId) throw new VehicleConflictException("Không thể chuyển dòng xe sang hãng khác; hãy tạo dòng xe mới.");
        var name = Name(request.Name);
        var normalized = VehicleRegistrationRules.NormalizeName(name);
        if (await db.VehicleModels.AnyAsync(x => x.Id != id && x.VehicleBrandId == model.VehicleBrandId && x.NormalizedName == normalized, ct))
            throw new VehicleConflictException("Tên dòng xe đã tồn tại trong hãng này.");
        model.Name = name; model.NormalizedName = normalized; model.IsActive = request.IsActive; model.Version++;
        await Save(ct);
        return new(model.Id, model.VehicleBrandId, model.Name, model.IsActive, model.Version);
    }

    internal static T ParseEnum<T>(string? value) where T : struct, Enum =>
        Enum.TryParse<T>(value, false, out var result) && Enum.IsDefined(result) && value == result.ToString()
            ? result : throw new VehicleValidationException(["Giá trị loại xe, nhiên liệu hoặc đơn vị ODO không hợp lệ."]);

    private static string Name(string? value) => value?.Trim() is { Length: > 0 and <= 150 } name
        ? name : throw new VehicleValidationException(["Tên danh mục cần từ 1 đến 150 ký tự."]);

    private async Task Save(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new VehicleConflictException("Phiên bản danh mục đã thay đổi."); }
        catch (DbUpdateException e) when (e.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { throw new VehicleConflictException("Tên danh mục đã tồn tại trong phạm vi này."); }
    }

    internal static string TypeLabel(VehicleType type) => type switch
    {
        VehicleType.Car => "Ô tô", VehicleType.Motorcycle => "Xe máy", VehicleType.Bicycle => "Xe đạp",
        VehicleType.Truck => "Xe tải", VehicleType.Bus => "Xe buýt", VehicleType.Van => "Xe van",
        VehicleType.Boat => "Tàu thuyền", VehicleType.Aircraft => "Máy bay", _ => "Khác"
    };
}
