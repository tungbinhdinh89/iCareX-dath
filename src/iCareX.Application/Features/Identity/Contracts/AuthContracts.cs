using iCareX.Shared.Contracts.Identity;

namespace iCareX.Application.Features.Identity.Contracts;

public interface IAuthService
{
    Task<AccountResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<(AccountResponse Account, Guid SessionId)?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
    Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken);
    Task RevokeSessionAsync(Guid accountId, Guid sessionId, CancellationToken cancellationToken);
}

public sealed class AuthConflictException(string message) : Exception(message);

public sealed class AuthValidationException(IReadOnlyCollection<string> errors) : Exception
{
    public IReadOnlyCollection<string> Errors { get; } = errors;
}
