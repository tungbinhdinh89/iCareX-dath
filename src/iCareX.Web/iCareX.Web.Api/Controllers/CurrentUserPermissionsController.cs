using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Web.Api.Controllers;

[ApiController, Authorize(Policy = "VerifiedAccount"), Route("api/v1/me/permissions")]
public sealed class CurrentUserPermissionsController(IVehicleAccessService access) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CurrentUserPermissionsResponse>> Get(CancellationToken ct) =>
        Ok(await access.GetCurrentPermissionsAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), ct));
}
