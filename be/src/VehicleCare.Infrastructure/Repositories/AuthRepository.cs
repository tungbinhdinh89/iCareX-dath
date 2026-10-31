using Microsoft.EntityFrameworkCore;
using VehicleCare.Application.Services.Contracts;
using VehicleCare.Domain;

namespace VehicleCare.Infrastructure.Repositories;

public sealed class AuthRepository(GarageDbContext db) : IAuthRepository
{
    public Task<Account?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.Accounts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == accountId, cancellationToken);

    public void AddAccount(Account account) => db.Accounts.Add(account);

    public void AddSettings(UserSettings settings) => db.Settings.Add(settings);

    public void AddSession(
        Guid accountId,
        Guid sessionId,
        string protectedTicket,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc) =>
        db.Sessions.Add(new AuthSession
        {
            Id = sessionId,
            AccountId = accountId,
            ProtectedTicket = protectedTicket,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = expiresAtUtc
        });

    public async Task<bool> RevokeSessionAsync(
        Guid accountId, Guid sessionId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken)
    {
        var session = await db.Sessions.SingleOrDefaultAsync(
            x => x.Id == sessionId && x.AccountId == accountId, cancellationToken);
        if (session is null)
            return false;
        session.RevokedAtUtc = revokedAtUtc;
        return true;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task<IAuthTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new AuthTransaction(await db.Database.BeginTransactionAsync(cancellationToken));

    private sealed class AuthTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
        : IAuthTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) =>
            transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
