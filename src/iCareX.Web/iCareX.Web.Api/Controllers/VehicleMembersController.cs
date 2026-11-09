using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Domain.Authorization;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Web.Api.Authorization;

namespace iCareX.Web.Api.Controllers;

[ApiController, Authorize(Policy = "VerifiedAccount")]
[Route("api/v1/vehicles/{vehicleId:guid}")]
[ServiceFilter(typeof(VehicleCsrfFilter))]
public sealed class VehicleMembersController(IVehicleAccessService access, IVehicleMemberService members) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("permissions"), Authorize(Policy = VehiclePermissions.VehicleRead)]
    public async Task<ActionResult<VehiclePermissionsResponse>> Permissions(Guid vehicleId, CancellationToken ct) =>
        Ok(await access.GetPermissionsAsync(Actor, vehicleId, ct));

    [HttpGet("members"), Authorize(Policy = VehiclePermissions.MemberRead)]
    public async Task<ActionResult<IReadOnlyCollection<VehicleMemberResponse>>> List(Guid vehicleId, CancellationToken ct) =>
        Ok(await members.ListAsync(Actor, vehicleId, ct));

    [HttpPatch("members/{userId:guid}"), Authorize(Policy = VehiclePermissions.MemberUpdate)]
    public async Task<ActionResult<VehicleMemberResponse>> Update(Guid vehicleId, Guid userId, UpdateVehicleMemberRequest request, CancellationToken ct) =>
        Ok(await members.UpdateAsync(Actor, vehicleId, userId, request, ct));

    [HttpDelete("members/{userId:guid}"), Authorize(Policy = VehiclePermissions.MemberRemove)]
    public async Task<IActionResult> Revoke(Guid vehicleId, Guid userId, [FromQuery] long version, CancellationToken ct)
    {
        await members.RevokeAsync(Actor, vehicleId, userId, version, ct);
        return NoContent();
    }
}
