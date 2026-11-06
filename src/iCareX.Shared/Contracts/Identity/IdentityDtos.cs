namespace iCareX.Shared.Contracts.Identity;

public sealed record RegisterRequest(string Name, string Email, string Password, string? Phone);

public sealed record LoginRequest(string Email, string Password);

public sealed record AccountResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    IReadOnlyCollection<string> Roles);

public sealed record CsrfResponse(string Token);
