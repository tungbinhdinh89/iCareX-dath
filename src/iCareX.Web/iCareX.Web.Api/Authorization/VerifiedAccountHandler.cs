using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Web.Api.Authorization;

public sealed record VerifiedAccountRequirement : IAuthorizationRequirement;

public sealed class VerifiedAccountHandler(GarageDbContext db) : AuthorizationHandler<VerifiedAccountRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, VerifiedAccountRequirement requirement)
    {
        if (Guid.TryParse(context.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)
            && await db.Users.AsNoTracking().AnyAsync(x => x.Id == actor && x.EmailConfirmed)) context.Succeed(requirement);
    }
}
