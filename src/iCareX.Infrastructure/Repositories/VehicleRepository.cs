using Microsoft.EntityFrameworkCore;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Repositories;

public sealed class VehicleRepository(GarageDbContext db)
{
    public Task<Vehicle?> FindAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken) =>
        db.Vehicles.Include(x => x.VehicleModel!).ThenInclude(x => x.VehicleBrand!).ThenInclude(x => x.VehicleType)
            .SingleOrDefaultAsync(x => x.AccountId == accountId && x.Id == vehicleId, cancellationToken);

    public Task<List<Vehicle>> ListAsync(Guid accountId, bool includeArchived, CancellationToken cancellationToken)
    {
        var query = db.Vehicles.Where(x => x.AccountId == accountId);
        if (!includeArchived) query = query.Where(x => x.ArchivedAtUtc == null);
        return query.Include(x => x.VehicleModel!).ThenInclude(x => x.VehicleBrand!).ThenInclude(x => x.VehicleType)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync(cancellationToken);
    }

    public Task<List<Entry>> ReadingsAsync(Guid accountId, Guid vehicleId, CancellationToken cancellationToken) =>
        db.Entries.Where(x => x.AccountId == accountId && x.VehicleId == vehicleId && x.Kind == EntryKind.Odo)
            .OrderByDescending(x => x.Date).ThenByDescending(x => x.Odo).ToListAsync(cancellationToken);

    public Task<Entry?> FindReadingAsync(Guid accountId, Guid vehicleId, Guid readingId, CancellationToken cancellationToken) =>
        db.Entries.SingleOrDefaultAsync(x => x.AccountId == accountId && x.VehicleId == vehicleId &&
            x.Id == readingId && x.Kind == EntryKind.Odo, cancellationToken);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    public void AddVehicle(Vehicle vehicle) => db.Vehicles.Add(vehicle);
    public void AddReading(Entry reading) => db.Entries.Add(reading);
    public void RemoveReading(Entry reading) => db.Entries.Remove(reading);
}
