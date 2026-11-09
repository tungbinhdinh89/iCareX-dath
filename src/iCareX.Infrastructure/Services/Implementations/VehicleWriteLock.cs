using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Services.Implementations;

internal static class VehicleWriteLock
{
    // All vehicle/member mutations use the same row lock. Re-check access after acquiring it.
    public static async Task<IDbContextTransaction> BeginAsync(GarageDbContext db, Guid vehicleId, CancellationToken cancellationToken)
    {
        var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (await db.Vehicles.FromSqlInterpolated(
                    $"SELECT * FROM vehicles WHERE \"Id\" = {vehicleId} FOR UPDATE")
                .AsNoTracking().SingleOrDefaultAsync(cancellationToken) is null)
                throw new VehicleNotFoundException();
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }
}
