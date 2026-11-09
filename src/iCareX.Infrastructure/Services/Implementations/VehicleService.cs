using System.Text.Json;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Domain.Authorization;
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
    IVehicleAccessService access,
    IBusinessClock clock,
    ILogger<VehicleService> logger) : IVehicleService
{
    public async Task<IReadOnlyCollection<VehicleResponse>> ListAsync(Guid accountId, bool includeArchived, CancellationToken cancellationToken)
    {
        var vehicles = await repository.ListAsync(accountId, includeArchived, cancellationToken);
        var result = new List<VehicleResponse>(vehicles.Count);
        foreach (var vehicle in vehicles)
        {
            await access.RequireAsync(accountId, vehicle.Id, VehiclePermissions.VehicleRead, cancellationToken);
            result.Add(ToResponse(vehicle, await repository.ReadingsAsync(vehicle.AccountId, vehicle.Id, cancellationToken)));
        }
        return result;
    }

    public async Task<VehicleResponse?> GetAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await repository.FindAsync(accountId, vehicleId, cancellationToken);
        if (vehicle is null) return null;
        await access.RequireAsync(accountId, vehicleId, VehiclePermissions.VehicleRead, cancellationToken);
        var readings = await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken);
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
            CreatedByUserId = accountId,
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
        db.VehicleMembers.Add(new VehicleMember
        {
            VehicleId = vehicle.Id,
            UserId = accountId,
            Role = VehicleMemberRole.Owner
        });
        Audit(accountId, vehicle, "vehicle.created");
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Vehicle created {VehicleId} for account {AccountId}", vehicle.Id, accountId);
        return ToResponse(vehicle, Array.Empty<OdometerReading>());
    }

    public async Task<VehicleResponse> UpdateAsync(Guid accountId, Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        ValidateVehicle(request.Name, request.Plate, request.Year, request.BaseOdo);
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        await access.RequireAsync(accountId, vehicleId, VehiclePermissions.VehicleUpdate, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        EnsureVersion(vehicle, request.Version);
        var model = await RequireActiveModel(request.VehicleModelId, cancellationToken);
        var readings = await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken);
        GarageRules.ValidateBaseOdo(request.BaseOdo, readings.Select(ToEntry));
        var normalizedPlate = GarageRules.NormalizePlate(request.Plate);
        if (await db.Vehicles.AnyAsync(x => x.AccountId == vehicle.AccountId && x.Id != vehicleId &&
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
        Audit(accountId, vehicle, "vehicle.changed");
        await SaveWithConcurrency(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Vehicle updated {VehicleId} for account {AccountId}", vehicleId, accountId);
        return ToResponse(vehicle, readings);
    }

    public Task<VehicleResponse> ArchiveAsync(Guid accountId, Guid vehicleId, long version, CancellationToken cancellationToken) =>
        SetArchivedAsync(accountId, vehicleId, version, true, cancellationToken);

    public Task<VehicleResponse> RestoreAsync(Guid accountId, Guid vehicleId, long version, CancellationToken cancellationToken) =>
        SetArchivedAsync(accountId, vehicleId, version, false, cancellationToken);

    public async Task<IReadOnlyCollection<OdometerReadingResponse>?> ListReadingsAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken)
    {
        var vehicle = await repository.FindAsync(accountId, vehicleId, cancellationToken);
        if (vehicle is null) return null;
        await access.RequireAsync(accountId, vehicleId, VehiclePermissions.VehicleRead, cancellationToken);
        return (await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken)).Select(ToReadingResponse).ToArray();
    }

    public async Task<OdometerReadingResponse> CreateReadingAsync(Guid accountId, Guid vehicleId, CreateOdometerReadingRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        await RequireOdoOwner(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        var reading = new OdometerReading
        {
            AccountId = vehicle.AccountId, VehicleId = vehicleId, CreatedByUserId = accountId,
            Date = request.Date, Odo = request.Odo,
            Note = request.Note?.Trim() ?? ""
        };
        var history = await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken);
        GarageRules.ValidateEntry(vehicle, ToEntry(reading), history.Select(ToEntry), clock.Today);
        repository.AddReading(reading);
        Audit(accountId, vehicle, "vehicle.odometer.created");
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("ODO reading created {ReadingId} for vehicle {VehicleId}", reading.Id, vehicleId);
        return ToReadingResponse(reading);
    }

    public async Task<OdometerReadingResponse> UpdateReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, UpdateOdometerReadingRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        await RequireOdoOwner(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        var reading = await repository.FindReadingAsync(vehicle.AccountId, vehicleId, readingId, cancellationToken)
            ?? throw new VehicleNotFoundException();
        EnsureVersion(reading, request.Version);
        var history = await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken);
        reading.Date = request.Date; reading.Odo = request.Odo; reading.Note = request.Note?.Trim() ?? "";
        GarageRules.ValidateEntry(vehicle, ToEntry(reading), history.Select(ToEntry), clock.Today);
        reading.Version++;
        reading.UpdatedAtUtc = clock.UtcNow;
        Audit(accountId, vehicle, "vehicle.changed");
        await SaveWithConcurrency(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToReadingResponse(reading);
    }

    public async Task DeleteReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, long version, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        await RequireOdoOwner(accountId, vehicleId, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        var reading = await repository.FindReadingAsync(vehicle.AccountId, vehicleId, readingId, cancellationToken)
            ?? throw new VehicleNotFoundException();
        EnsureVersion(reading, version);
        repository.RemoveReading(reading);
        Audit(accountId, vehicle, "vehicle.changed");
        await SaveWithConcurrency(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("ODO reading deleted {ReadingId} for vehicle {VehicleId}", readingId, vehicleId);
    }

    private async Task<VehicleResponse> SetArchivedAsync(Guid accountId, Guid vehicleId, long version, bool archived, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        await access.RequireAsync(accountId, vehicleId, archived ? VehiclePermissions.VehicleArchive : VehiclePermissions.VehicleRestore, cancellationToken);
        EnsureVersion(vehicle, version);
        vehicle.ArchivedAtUtc = archived ? clock.UtcNow : null;
        vehicle.Version++;
        vehicle.UpdatedAtUtc = clock.UtcNow;
        Audit(accountId, vehicle, "vehicle.changed");
        await SaveWithConcurrency(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Vehicle {VehicleId} archive state changed to {Archived}", vehicleId, archived);
        return ToResponse(vehicle, await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken));
    }

    private async Task RequireOdoOwner(Guid actor, Guid vehicleId, CancellationToken ct)
    {
        var member = await access.RequireAsync(actor, vehicleId, VehiclePermissions.VehicleRead, ct);
        if (member.Role != VehicleMemberRole.Owner) throw new VehicleAccessDeniedException();
    }

    private void Audit(Guid actor, Vehicle vehicle, string action) => db.AuditLogs.Add(new AuditLog
    {
        ActorUserId = actor, VehicleId = vehicle.Id, Action = action, EntityType = nameof(Vehicle),
        EntityId = vehicle.Id, CreatedAtUtc = clock.UtcNow,
        NewValues = JsonSerializer.Serialize(new { vehicle.Version, archived = vehicle.ArchivedAtUtc != null })
    });

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
            entity is OdometerReading reading && reading.Version != version)
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

    private static VehicleResponse ToResponse(Vehicle vehicle, IEnumerable<OdometerReading> readings) =>
        new(vehicle.Id, vehicle.Name, vehicle.Plate,
            vehicle.VehicleModel!.VehicleBrand!.VehicleType!.Id,
            vehicle.VehicleModel.VehicleBrand.VehicleType.Name,
            vehicle.VehicleModel.VehicleBrand.Id,
            vehicle.VehicleModel.VehicleBrand.Name,
            vehicle.VehicleModel.Id,
            vehicle.VehicleModel.Name,
            vehicle.Year,
            vehicle.BaseOdo, GarageRules.CurrentOdo(vehicle, readings.Select(ToEntry)), vehicle.ImageUrl,
            vehicle.ArchivedAtUtc is not null, vehicle.Version);

    private static OdometerReadingResponse ToReadingResponse(OdometerReading reading) =>
        new(reading.Id, reading.VehicleId, reading.Date, reading.Odo, "Cập nhật ODO", reading.Note, reading.Version);

    private static Entry ToEntry(OdometerReading reading) => new()
    {
        Id = reading.Id, AccountId = reading.AccountId, VehicleId = reading.VehicleId,
        Kind = EntryKind.Odo, Date = reading.Date, Odo = reading.Odo, Note = reading.Note,
        CreatedAtUtc = reading.CreatedAtUtc, UpdatedAtUtc = reading.UpdatedAtUtc, Version = reading.Version
    };
}
