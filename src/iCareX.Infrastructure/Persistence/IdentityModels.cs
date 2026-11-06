using Microsoft.AspNetCore.Identity;

namespace iCareX.Infrastructure.Persistence;

public sealed class ApplicationUser : IdentityUser<Guid> { }

public sealed class AuthSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AccountId { get; set; }
    public string ProtectedTicket { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
}

public sealed class IdempotencyRecord
{
    public Guid AccountId { get; set; }
    public string Key { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public int ResponseStatus { get; set; }
    public string ResponseBody { get; set; } = "";
    public string ResponseETag { get; set; } = "";
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
