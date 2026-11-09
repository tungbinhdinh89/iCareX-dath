using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Web.Api;

internal sealed class SessionCookieEvents(GarageDbContext db, TimeProvider timeProvider) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal is null ||
            !Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) ||
            !Guid.TryParse(principal.FindFirstValue("sid"), out var sessionId))
        {
            context.RejectPrincipal();
            return;
        }

        var now = timeProvider.GetUtcNow();
        var valid = await db.Sessions.AsNoTracking().AnyAsync(
            session => session.Id == sessionId &&
                       session.AccountId == accountId &&
                       session.RevokedAtUtc == null &&
                       session.ExpiresAtUtc > now &&
                       db.Users.Any(user => user.Id == accountId &&
                           (!user.LockoutEnabled || user.LockoutEnd == null || user.LockoutEnd <= now)),
            context.HttpContext.RequestAborted);
        if (!valid)
            context.RejectPrincipal();
    }
}
