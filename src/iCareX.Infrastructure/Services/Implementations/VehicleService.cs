using iCareX.Domain.Enums;
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
        if (!await db.Users.AsNoTracking().AnyAsync(x => x.Id == accountId && x.EmailConfirmed
            && (!x.LockoutEnabled || x.LockoutEnd == null || x.LockoutEnd <= clock.UtcNow), cancellationToken))
            throw new VehicleAccessDeniedException();
        var vehicle = Candidate(request);
        ValidateVehicle(vehicle);
        vehicle.VehicleModel = await RequireModel(vehicle, true, cancellationToken);
        vehicle.VehicleBrand = vehicle.VehicleModel.VehicleBrand;
        vehicle.AccountId = accountId;
        vehicle.CreatedByUserId = accountId;
        await EnsureUniquePlate(vehicle.AccountId, vehicle.Id, vehicle.PlateNormalized, cancellationToken);
        repository.AddVehicle(vehicle);
        db.VehicleMembers.Add(new VehicleMember
        {
            VehicleId = vehicle.Id,
            UserId = accountId,
            Role = VehicleMemberRole.Owner
        });
        Audit(accountId, vehicle, "vehicle.created");
        await SaveWithConcurrency(cancellationToken);
        logger.LogInformation("Vehicle created {VehicleId} for account {AccountId}", vehicle.Id, accountId);
        return ToResponse(vehicle, Array.Empty<OdometerReading>());
    }

    public async Task<VehicleResponse> UpdateAsync(Guid accountId, Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        var vehicle = await RequireVehicle(accountId, vehicleId, cancellationToken);
        await access.RequireAsync(accountId, vehicleId, VehiclePermissions.VehicleUpdate, cancellationToken);
        GarageRules.EnsureActive(vehicle);
        EnsureVersion(vehicle, request.Version);
        var candidate = Candidate(new CreateVehicleRequest(request.VehicleType, request.VehicleBrandId,
            request.VehicleModelId, request.FuelType, request.OdometerUnit, request.Odometer,
            request.Year, request.Name, request.Plate, request.Vin, request.RegistrationId, request.FuelCapacity,
            request.PurchasePrice, request.SalePrice, request.Notes, request.ImageUrl));
        ValidateVehicle(candidate);
        var changedCatalog = candidate.VehicleModelId != vehicle.VehicleModelId || candidate.VehicleBrandId != vehicle.VehicleBrandId
            || candidate.VehicleType != vehicle.VehicleType;
        var model = await RequireModel(candidate, changedCatalog, cancellationToken);
        var readings = await repository.ReadingsAsync(vehicle.AccountId, vehicleId, cancellationToken);
        GarageRules.ValidateBaseOdo(candidate.Odometer, readings.Select(ToEntry));
        if (candidate.OdometerUnit != vehicle.OdometerUnit && (readings.Count != 0
            || await db.FuelEntries.AnyAsync(x => x.VehicleId == vehicleId, cancellationToken)
            || await db.MaintenanceRecords.AnyAsync(x => x.VehicleId == vehicleId, cancellationToken)
            || await db.Plans.AnyAsync(x => x.VehicleId == vehicleId, cancellationToken)
            || await db.Reminders.AnyAsync(x => x.VehicleId == vehicleId, cancellationToken)))
            throw new VehicleConflictException("Không thể đổi đơn vị ODO khi xe đã có lịch sử hoặc kế hoạch liên quan.");
        await EnsureUniquePlate(vehicle.AccountId, vehicle.Id, candidate.PlateNormalized, cancellationToken);
        ApplyDetails(vehicle, candidate);
        vehicle.VehicleModel = model;
        vehicle.VehicleBrand = model.VehicleBrand;
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
        catch (DbUpdateException error) when (error.InnerException is Npgsql.PostgresException { SqlState: "23505" }) { throw new VehicleConflictException("Biển số đã tồn tại trong tài khoản."); }
        catch (DbUpdateConcurrencyException) { throw new VehicleConflictException("Dữ liệu đã thay đổi. Vui lòng tải lại."); }
    }

    private static void EnsureVersion(AuditedEntity entity, long version)
    {
        if (entity is Vehicle vehicle && vehicle.Version != version ||
            entity is OdometerReading reading && reading.Version != version)
            throw new VehicleConflictException("Phiên bản dữ liệu không còn mới.");
    }

    private async Task<VehicleModel> RequireModel(Vehicle candidate, bool requireActive, CancellationToken ct) =>
        await db.VehicleModels.Include(x => x.VehicleBrand).SingleOrDefaultAsync(x => x.Id == candidate.VehicleModelId
            && x.VehicleBrandId == candidate.VehicleBrandId && x.VehicleBrand!.VehicleType == candidate.VehicleType
            && (!requireActive || x.IsActive && x.VehicleBrand.IsActive), ct)
            ?? throw new VehicleValidationException(["Loại, hãng và dòng xe không khớp hoặc catalog mới chọn đã ngừng sử dụng."]);

    private async Task EnsureUniquePlate(Guid owner, Guid vehicleId, string? plate, CancellationToken ct)
    {
        if (plate is not null && await db.Vehicles.AnyAsync(x => x.AccountId == owner && x.Id != vehicleId && x.PlateNormalized == plate, ct))
            throw new VehicleConflictException("Biển số đã tồn tại trong tài khoản.");
    }

    private void ValidateVehicle(Vehicle vehicle)
    {
        try { VehicleRegistrationRules.Validate(vehicle, clock.Today.Year); }
        catch (iCareX.Domain.Exceptions.RuleViolation error) { throw new VehicleValidationException([error.Message]); }
    }

    private static Vehicle Candidate(CreateVehicleRequest r) => new()
    {
        VehicleType = VehicleCatalogService.ParseEnum<VehicleType>(r.VehicleType),
        VehicleBrandId = r.VehicleBrandId, VehicleModelId = r.VehicleModelId,
        FuelType = VehicleCatalogService.ParseEnum<FuelType>(r.FuelType),
        OdometerUnit = VehicleCatalogService.ParseEnum<OdometerUnit>(r.OdometerUnit),
        Odometer = r.Odometer, Year = r.Year,
        Name = VehicleRegistrationRules.OptionalText(r.Name),
        Plate = VehicleRegistrationRules.OptionalText(r.Plate)?.ToUpperInvariant(),
        PlateNormalized = VehicleRegistrationRules.NormalizePlate(r.Plate),
        VIN = VehicleRegistrationRules.NormalizeVin(r.Vin), RegistrationId = VehicleRegistrationRules.OptionalText(r.RegistrationId),
        FuelCapacity = r.FuelCapacity, PurchasePrice = r.PurchasePrice, SalePrice = r.SalePrice,
        Notes = VehicleRegistrationRules.OptionalText(r.Notes), ImageUrl = VehicleRegistrationRules.OptionalText(r.ImageUrl)
    };

    private static void ApplyDetails(Vehicle target, Vehicle source)
    {
        target.VehicleType = source.VehicleType; target.VehicleBrandId = source.VehicleBrandId; target.VehicleModelId = source.VehicleModelId;
        target.FuelType = source.FuelType; target.OdometerUnit = source.OdometerUnit; target.Odometer = source.Odometer;
        target.Year = source.Year; target.Name = source.Name;
        target.Plate = source.Plate; target.PlateNormalized = source.PlateNormalized; target.VIN = source.VIN;
        target.RegistrationId = source.RegistrationId; target.FuelCapacity = source.FuelCapacity;
        target.PurchasePrice = source.PurchasePrice; target.SalePrice = source.SalePrice;
        target.Notes = source.Notes; target.ImageUrl = source.ImageUrl;
    }

    private static VehicleResponse ToResponse(Vehicle v, IEnumerable<OdometerReading> readings) =>
        new(v.Id, v.Name, v.Plate, v.VehicleType.ToString(), VehicleCatalogService.TypeLabel(v.VehicleType),
            v.VehicleBrandId, v.VehicleModel!.VehicleBrand!.Name, v.VehicleModelId, v.VehicleModel.Name,
            v.Year, v.Odometer, GarageRules.CurrentOdo(v, readings.Select(ToEntry)), v.FuelType.ToString(), v.FuelCapacity,
            v.OdometerUnit.ToString(), v.VIN, v.RegistrationId, v.PurchasePrice, v.SalePrice,
            v.Notes, v.ImageUrl, v.ArchivedAtUtc is not null, v.Version, v.CreatedAtUtc, v.UpdatedAtUtc);

    private static OdometerReadingResponse ToReadingResponse(OdometerReading reading) =>
        new(reading.Id, reading.VehicleId, reading.Date, reading.Odo, "Cập nhật ODO", reading.Note, reading.Version);

    private static Entry ToEntry(OdometerReading reading) => new()
    {
        Id = reading.Id, AccountId = reading.AccountId, VehicleId = reading.VehicleId,
        Kind = EntryKind.Odo, Date = reading.Date, Odo = reading.Odo, Note = reading.Note,
        CreatedAtUtc = reading.CreatedAtUtc, UpdatedAtUtc = reading.UpdatedAtUtc, Version = reading.Version
    };
}
