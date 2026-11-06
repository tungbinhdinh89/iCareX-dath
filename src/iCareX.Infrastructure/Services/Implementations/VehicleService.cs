using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using iCareX.Application.Abstractions;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Domain.Entities;
using iCareX.Domain.Common;
using iCareX.Infrastructure.Persistence;
using iCareX.Infrastructure.Repositories;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class VehicleService(
    VehicleRepository repository,
    GarageDbContext db,
    IBusinessClock clock,
    ILogger<VehicleService> logger) : IVehicleService
{
    public async Task<IReadOnlyCollection<VehicleResponse>> ListAsync(Guid accountId, bool includeArchived, CancellationToken cancellationToken)
    {
        var vehicles = await repository.ListAsync(accountId, includeArchived, cancellationToken);
        var result = new List<VehicleResponse>(vehicles.Count);
        foreach (var vehicle in vehicles)
            result.Add(ToResponse(vehicle, await repository.ReadingsAsync(accountId, vehicle.Id, cancellationToken)));
        return result;
    }

    public async Task<VehicleResponse?> GetAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await repository.FindAsync(accountId, vehicleId, cancellationToken);
        if (vehicle is null) return null;
        var readings = await repository.ReadingsAsync(accountId, vehicleId, cancellationToken);
        return ToResponse(vehicle, readings);
    }

    public async Task<VehicleResponse> CreateAsync(Guid accountId, CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        ValidateVehicle(request.Name, request.Plate, request.Year, request.BaseOdo);
        var model = await RequireActiveModel(request.VehicleModelId, cancellationToken);
        var normalizedPlate = GarageRules.NormalizePlate(request.Plate);
        if (await db.Vehicles.AnyAsync(x => x.AccountId == accountId && x.PlateNormalized == normalizedPlate, cancellationToken))
            throw new VehicleConflictException("Biển số đã tồn tại trong tài khoản.");
        var vehicle = new Vehicle
        {
            AccountId = accountId,
            Name = request.Name.Trim(),
            Plate = request.Plate.Trim().ToUpperInvariant(),
            PlateNormalized = normalizedPlate,
            VehicleModelId = model.Id,
            VehicleModel = model,
            Year = request.Year,
            BaseOdo = request.BaseOdo,
            ImageUrl = request.ImageUrl?.Trim() ?? ""
        };
        repository.AddVehicle(vehicle);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Vehicle created {VehicleId} for account {AccountId}", vehicle.Id, accountId);
        return ToResponse(vehicle, Array.Empty<Entry>());
    }

    public async Task<VehicleResponse> UpdateAsync(Guid accountId, Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        ValidateVehicle(request.Name, request.Plate, request.Year, request.BaseOdo);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        EnsureVersion(vehicle, request.Version);
        var model = await RequireActiveModel(request.VehicleModelId, cancellationToken);
        var readings = await repository.ReadingsAsync(accountId, vehicleId, cancellationToken);
        GarageRules.ValidateBaseOdo(request.BaseOdo, readings);
        var normalizedPlate = GarageRules.NormalizePlate(request.Plate);
        if (await db.Vehicles.AnyAsync(x => x.AccountId == accountId && x.Id != vehicleId &&
            x.PlateNormalized == normalizedPlate, cancellationToken))
            throw new VehicleConflictException("Biển số đã tồn tại trong tài khoản.");
        vehicle.Name = request.Name.Trim();
        vehicle.Plate = request.Plate.Trim().ToUpperInvariant();
        vehicle.PlateNormalized = normalizedPlate;
        vehicle.VehicleModelId = model.Id;
        vehicle.VehicleModel = model;
        vehicle.Year = request.Year;
        vehicle.BaseOdo = request.BaseOdo;
        vehicle.ImageUrl = request.ImageUrl?.Trim() ?? "";
        vehicle.Version++;
        vehicle.UpdatedAtUtc = clock.UtcNow;
        await SaveWithConcurrency(cancellationToken);
        logger.LogInformation("Vehicle updated {VehicleId} for account {AccountId}", vehicleId, accountId);
        return ToResponse(vehicle, readings);
    }

    public Task<VehicleResponse> ArchiveAsync(Guid accountId, Guid vehicleId, long version, CancellationToken cancellationToken) =>
        SetArchivedAsync(accountId, vehicleId, version, true, cancellationToken);

    public Task<VehicleResponse> RestoreAsync(Guid accountId, Guid vehicleId, long version, CancellationToken cancellationToken) =>
        SetArchivedAsync(accountId, vehicleId, version, false, cancellationToken);

    public async Task<IReadOnlyCollection<OdometerReadingResponse>?> ListReadingsAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken)
    {
        if (await repository.FindAsync(accountId, vehicleId, cancellationToken) is null) return null;
        return (await repository.ReadingsAsync(accountId, vehicleId, cancellationToken)).Select(ToReadingResponse).ToArray();
    }

    public async Task<OdometerReadingResponse> CreateReadingAsync(Guid accountId, Guid vehicleId, CreateOdometerReadingRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        var reading = new Entry
        {
            AccountId = accountId, VehicleId = vehicleId, Kind = EntryKind.Odo,
            Title = "Cập nhật ODO", Date = request.Date, Odo = request.Odo,
            Note = request.Note?.Trim() ?? ""
        };
        var history = await repository.ReadingsAsync(accountId, vehicleId, cancellationToken);
        GarageRules.ValidateEntry(vehicle, reading, history, clock.Today);
        repository.AddReading(reading);
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("ODO reading created {ReadingId} for vehicle {VehicleId}", reading.Id, vehicleId);
        return ToReadingResponse(reading);
    }

    public async Task<OdometerReadingResponse> UpdateReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, UpdateOdometerReadingRequest request, CancellationToken cancellationToken)
    {
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        var reading = await repository.FindReadingAsync(accountId, vehicleId, readingId, cancellationToken)
            ?? throw new VehicleNotFoundException();
        EnsureVersion(reading, request.Version);
        var history = await repository.ReadingsAsync(accountId, vehicleId, cancellationToken);
        reading.Date = request.Date; reading.Odo = request.Odo; reading.Note = request.Note?.Trim() ?? "";
        GarageRules.ValidateEntry(vehicle, reading, history, clock.Today);
        reading.Version++;
        reading.UpdatedAtUtc = clock.UtcNow;
        await SaveWithConcurrency(cancellationToken);
        return ToReadingResponse(reading);
    }

    public async Task DeleteReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, long version, CancellationToken cancellationToken)
    {
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        var reading = await repository.FindReadingAsync(accountId, vehicleId, readingId, cancellationToken)
            ?? throw new VehicleNotFoundException();
        EnsureVersion(reading, version);
        repository.RemoveReading(reading);
        await SaveWithConcurrency(cancellationToken);
        logger.LogInformation("ODO reading deleted {ReadingId} for vehicle {VehicleId}", readingId, vehicleId);
    }

    private async Task<VehicleResponse> SetArchivedAsync(Guid accountId, Guid vehicleId, long version, bool archived, CancellationToken cancellationToken)
    {
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        EnsureVersion(vehicle, version);
        vehicle.ArchivedAtUtc = archived ? clock.UtcNow : null;
        vehicle.Version++;
        vehicle.UpdatedAtUtc = clock.UtcNow;
        await SaveWithConcurrency(cancellationToken);
        logger.LogInformation("Vehicle {VehicleId} archive state changed to {Archived}", vehicleId, archived);
        return ToResponse(vehicle, await repository.ReadingsAsync(accountId, vehicleId, cancellationToken));
    }

    private async Task<Vehicle> RequireVehicle(Guid accountId, Guid vehicleId, CancellationToken cancellationToken) =>
        await repository.FindAsync(accountId, vehicleId, cancellationToken) ?? throw new VehicleNotFoundException();

    private async Task SaveWithConcurrency(CancellationToken cancellationToken)
    {
        try { await repository.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new VehicleConflictException("Dữ liệu đã thay đổi. Vui lòng tải lại."); }
    }

    private static void EnsureVersion(AuditedEntity entity, long version)
    {
        if (entity is Vehicle vehicle && vehicle.Version != version ||
            entity is Entry entry && entry.Version != version)
            throw new VehicleConflictException("Phiên bản dữ liệu không còn mới.");
    }

    private async Task<VehicleModel> RequireActiveModel(Guid modelId, CancellationToken cancellationToken) =>
        await db.VehicleModels.Include(x => x.VehicleBrand!).ThenInclude(x => x.VehicleType)
            .SingleOrDefaultAsync(x => x.Id == modelId && x.IsActive &&
                x.VehicleBrand!.IsActive && x.VehicleBrand.VehicleType!.IsActive, cancellationToken)
            ?? throw new VehicleValidationException(["Hãng và dòng xe không hợp lệ hoặc đã ngừng sử dụng."]);

    private static void ValidateVehicle(string name, string plate, int year, decimal baseOdo)
    {
        if (name.Trim().Length is < 2 or > 60 || plate.Trim().Length is < 2 or > 20 ||
            year is < 1980 or > 2100)
            throw new VehicleValidationException(["Thông tin phương tiện không hợp lệ."]);
        GarageRules.Number(baseOdo, 0, 9_999_999, "ODO ban đầu");
        if (GarageRules.NormalizePlate(plate).Length < 2)
            throw new VehicleValidationException(["Biển số không hợp lệ."]);
    }

    private static VehicleResponse ToResponse(Vehicle vehicle, IEnumerable<Entry> readings) =>
        new(vehicle.Id, vehicle.Name, vehicle.Plate,
            vehicle.VehicleModel!.VehicleBrand!.VehicleType!.Id,
            vehicle.VehicleModel.VehicleBrand.VehicleType.Name,
            vehicle.VehicleModel.VehicleBrand.Id,
            vehicle.VehicleModel.VehicleBrand.Name,
            vehicle.VehicleModel.Id,
            vehicle.VehicleModel.Name,
            vehicle.Year,
            vehicle.BaseOdo, GarageRules.CurrentOdo(vehicle, readings), vehicle.ImageUrl,
            vehicle.ArchivedAtUtc is not null, vehicle.Version);

    private static OdometerReadingResponse ToReadingResponse(Entry reading) =>
        new(reading.Id, reading.VehicleId, reading.Date, reading.Odo, reading.Title, reading.Note, reading.Version);
}
