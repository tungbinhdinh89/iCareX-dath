using Microsoft.EntityFrameworkCore;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Repositories;

public sealed class VehicleRepository(GarageDbContext db)
{
    public Task<Vehicle?> FindAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken) =>
        db.Vehicles.Include(x => x.VehicleModel!).ThenInclude(x => x.VehicleBrand!)
            .SingleOrDefaultAsync(x => x.Id == vehicleId && db.VehicleMembers.Any(m => m.VehicleId == x.Id && m.UserId == accountId && m.RevokedAtUtc == null), cancellationToken);

    public Task<List<Vehicle>> ListAsync(Guid accountId, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = db.Vehicles.Where(x => db.VehicleMembers.Any(m => m.VehicleId == x.Id && m.UserId == accountId && m.RevokedAtUtc == null));
        if (!includeArchived) query = query.Where(x => x.ArchivedAtUtc == null);
        return query.Include(x => x.VehicleModel!).ThenInclude(x => x.VehicleBrand!)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public Task<List<OdometerReading>> ReadingsAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken) =>
        db.OdometerReadings.Where(x => x.AccountId == accountId && x.VehicleId == vehicleId)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Odo).ToListAsync(cancellationToken);

    public Task<OdometerReading?> FindReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, CancellationToken cancellationToken) =>
        db.OdometerReadings.SingleOrDefaultAsync(x => x.AccountId == accountId && x.VehicleId == vehicleId &&
            x.Id == readingId, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public void AddVehicle(Vehicle vehicle) => db.Vehicles.Add(vehicle);
    public void AddReading(OdometerReading reading) => db.OdometerReadings.Add(reading);
    public void RemoveReading(OdometerReading reading) => db.OdometerReadings.Remove(reading);
}
