using System.Text.Json.Serialization;

namespace iCareX.Shared.Contracts.Identity;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record RegisterRequest(string Name, string Email, string Password, string? Phone);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record LoginRequest(string Email, string Password);

public sealed record AccountResponse(
    Guid Id,
    string Name,
    string Email,
    string Phone,
    IReadOnlyCollection<string> Roles,
    string AccountStatus,
    bool EmailVerified);

public sealed record CsrfResponse(string Token);

// Flat account fields preserve existing registration consumers; delivery is not verification.
public sealed record RegistrationResponse(
    Guid Id, string Name, string Email, string Phone, IReadOnlyCollection<string> Roles,
    string AccountStatus, bool EmailVerified, bool VerificationEmailSent);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record EmailActionRequest(string Email);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record VerifyEmailRequest(string Email, string Token);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AccountActionAcceptedResponse(string Message);
