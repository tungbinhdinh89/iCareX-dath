using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Domain.Authorization;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Web.Api.Authorization;

namespace iCareX.Web.Api.Controllers;

[ApiController, Authorize(Policy = "VerifiedAccount")]
[Route("api/v1/vehicles/{vehicleId:guid}/invitations")]
[ServiceFilter(typeof(VehicleCsrfFilter))]
[EnableRateLimiting("auth-actions")]
public sealed class VehicleInvitationsController(IVehicleInvitationService invitations) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet, Authorize(Policy = VehiclePermissions.VehicleRead)]
    public async Task<ActionResult<IReadOnlyCollection<VehicleInvitationResponse>>> List(Guid vehicleId, CancellationToken ct) =>
        Ok(await invitations.ListAsync(Actor, vehicleId, ct));
    [HttpPost, Authorize(Policy = VehiclePermissions.MemberInvite)]
    public async Task<ActionResult<VehicleInvitationResponse>> Create(Guid vehicleId, CreateVehicleInvitationRequest request, CancellationToken ct)
    {
        var result = await invitations.CreateAsync(Actor, vehicleId, request, ct);
        return Created($"/api/v1/vehicles/{vehicleId}/invitations/{result.Id}", result);
    }
    [HttpDelete("{invitationId:guid}"), Authorize(Policy = VehiclePermissions.MemberRemove)]
    public async Task<IActionResult> Revoke(Guid vehicleId, Guid invitationId, [FromQuery] long version, CancellationToken ct)
    {
        await invitations.RevokeAsync(Actor, vehicleId, invitationId, version, ct);
        return NoContent();
    }
}

[ApiController, Route("api/v1/invitations")]
[ServiceFilter(typeof(VehicleCsrfFilter))]
[EnableRateLimiting("auth-actions")]
public sealed class InvitationsController(IVehicleInvitationService invitations) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    [HttpGet("inbox"), Authorize(Policy = "VerifiedAccount")]
    public async Task<ActionResult<IReadOnlyCollection<VehicleInvitationResponse>>> Inbox(CancellationToken ct) =>
        Ok(await invitations.InboxAsync(Actor, ct));
    [HttpPost("{invitationId:guid}/preview")]
    public async Task<ActionResult<VehicleInvitationResponse>> Preview(Guid invitationId, InvitationTokenRequest request, CancellationToken ct) =>
        Ok(await invitations.PreviewAsync(invitationId, request.Token, ct));
    [HttpPost("{invitationId:guid}/accept"), Authorize(Policy = "VerifiedAccount")]
    public async Task<IActionResult> Accept(Guid invitationId, InvitationTokenRequest request, CancellationToken ct)
    {
        await invitations.RespondAsync(Actor, invitationId, request.Token, true, ct);
        return NoContent();
    }
    [HttpPost("{invitationId:guid}/reject"), Authorize(Policy = "VerifiedAccount")]
    public async Task<IActionResult> Reject(Guid invitationId, InvitationTokenRequest request, CancellationToken ct)
    {
        await invitations.RespondAsync(Actor, invitationId, request.Token, false, ct);
        return NoContent();
    }
}
