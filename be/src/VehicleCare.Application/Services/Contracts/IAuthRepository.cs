using VehicleCare.Domain;

namespace VehicleCare.Application.Services.Contracts;

public interface IAuthRepository
{
    Task<Account?> FindAccountAsync(Guid accountId, CancellationToken cancellationToken);
    void AddAccount(Account account);
    void AddSettings(UserSettings settings);
    void AddSession(
        Guid accountId,
        Guid sessionId,
        string protectedTicket,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc);
    Task<bool> RevokeSessionAsync(Guid accountId, Guid sessionId, DateTimeOffset revokedAtUtc, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IAuthTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

public interface IAuthTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}
