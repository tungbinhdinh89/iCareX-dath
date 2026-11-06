using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;
using iCareX.Domain.Entities;
using iCareX.Domain.Exceptions;

namespace iCareX.Web.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vehicles")]
public sealed class VehiclesController(IVehicleService vehicles) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<VehicleListResponse>> List(
        [FromQuery] bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var result = await vehicles.ListAsync(AccountId(), includeArchived, cancellationToken);
        return Ok(new VehicleListResponse(result));
    }

    [HttpGet("{vehicleId:guid}")]
    public async Task<ActionResult<VehicleResponse>> Get(Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await vehicles.GetAsync(AccountId(), vehicleId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<VehicleResponse>> Create(
        CreateVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await vehicles.CreateAsync(AccountId(), request, cancellationToken);
            return CreatedAtAction(nameof(Get), new { vehicleId = result.Id }, result);
        }
        catch (VehicleValidationException exception) { return Validation(exception.Errors); }
        catch (VehicleConflictException exception) { return Conflict(new ProblemDetails { Title = "Vehicle conflict.", Detail = exception.Message }); }
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<ActionResult<VehicleResponse>> Update(
        Guid vehicleId, UpdateVehicleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await vehicles.UpdateAsync(AccountId(), vehicleId, request, cancellationToken);
            return Ok(result);
        }
        catch (VehicleNotFoundException) { return NotFound(); }
        catch (VehicleValidationException exception) { return Validation(exception.Errors); }
        catch (VehicleConflictException exception) { return Conflict(new ProblemDetails { Title = "Vehicle conflict.", Detail = exception.Message }); }
    }

    [HttpPost("{vehicleId:guid}/archive")]
    public Task<ActionResult<VehicleResponse>> Archive(Guid vehicleId, VersionRequest request, CancellationToken cancellationToken) =>
        ChangeArchive(vehicleId, request.Version, true, cancellationToken);

    [HttpPost("{vehicleId:guid}/restore")]
    public Task<ActionResult<VehicleResponse>> Restore(Guid vehicleId, VersionRequest request, CancellationToken cancellationToken) =>
        ChangeArchive(vehicleId, request.Version, false, cancellationToken);

    [HttpGet("{vehicleId:guid}/odometer-readings")]
    public async Task<ActionResult<IReadOnlyCollection<OdometerReadingResponse>>> Readings(
        Guid vehicleId, CancellationToken cancellationToken)
    {
        var result = await vehicles.ListReadingsAsync(AccountId(), vehicleId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{vehicleId:guid}/odometer-readings")]
    public async Task<ActionResult<OdometerReadingResponse>> CreateReading(
        Guid vehicleId, CreateOdometerReadingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await vehicles.CreateReadingAsync(AccountId(), vehicleId, request, cancellationToken);
            return CreatedAtAction(nameof(Readings), new { vehicleId }, result);
        }
        catch (VehicleNotFoundException) { return NotFound(); }
        catch (VehicleValidationException exception) { return Validation(exception.Errors); }
        catch (VehicleConflictException exception) { return Conflict(new ProblemDetails { Title = "ODO conflict.", Detail = exception.Message }); }
        catch (RuleViolation exception) { return Validation([exception.Message]); }
    }

    [HttpPut("{vehicleId:guid}/odometer-readings/{readingId:guid}")]
    public async Task<ActionResult<OdometerReadingResponse>> UpdateReading(
        Guid vehicleId, Guid readingId, UpdateOdometerReadingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await vehicles.UpdateReadingAsync(AccountId(), vehicleId, readingId, request, cancellationToken));
        }
        catch (VehicleNotFoundException) { return NotFound(); }
        catch (VehicleValidationException exception) { return Validation(exception.Errors); }
        catch (VehicleConflictException exception) { return Conflict(new ProblemDetails { Title = "ODO conflict.", Detail = exception.Message }); }
        catch (RuleViolation exception) { return Validation([exception.Message]); }
    }

    [HttpDelete("{vehicleId:guid}/odometer-readings/{readingId:guid}")]
    public async Task<IActionResult> DeleteReading(
        Guid vehicleId, Guid readingId, [FromQuery] long version, CancellationToken cancellationToken)
    {
        try
        {
            await vehicles.DeleteReadingAsync(AccountId(), vehicleId, readingId, version, cancellationToken);
            return NoContent();
        }
        catch (VehicleNotFoundException) { return NotFound(); }
        catch (VehicleConflictException exception) { return Conflict(new ProblemDetails { Title = "ODO conflict.", Detail = exception.Message }); }
        catch (RuleViolation exception) { return Validation([exception.Message]); }
    }

    private async Task<ActionResult<VehicleResponse>> ChangeArchive(
        Guid vehicleId, long version, bool archive, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(archive
                ? await vehicles.ArchiveAsync(AccountId(), vehicleId, version, cancellationToken)
                : await vehicles.RestoreAsync(AccountId(), vehicleId, version, cancellationToken));
        }
        catch (VehicleNotFoundException) { return NotFound(); }
        catch (VehicleConflictException exception) { return Conflict(new ProblemDetails { Title = "Vehicle conflict.", Detail = exception.Message }); }
    }

    private ActionResult Validation(IReadOnlyCollection<string> errors) =>
        BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["vehicle"] = errors.ToArray() }));

    private Guid AccountId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Authenticated account claim is missing.");
}
