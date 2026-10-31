namespace VehicleCare.Application.Services.Contracts;

public sealed record RegisterRequest(string Name, string Email, string Password, string? Phone);

public sealed record LoginRequest(string Email, string Password);

public sealed record AccountResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    IReadOnlyCollection<string> Roles);

public sealed record CsrfResponse(string Token);

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
