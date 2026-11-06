using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Web.Api.Controllers;

[ApiController]
[Route("api/v1/vehicle-catalog")]
public sealed class VehicleCatalogController(IVehicleCatalogService catalog) : ControllerBase
{
    [HttpGet("types")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<VehicleTypeResponse>> Types(CancellationToken cancellationToken) =>
        catalog.ListTypesAsync(cancellationToken);

    [HttpGet("types/{vehicleTypeId:guid}/brands")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<VehicleBrandResponse>> Brands(Guid vehicleTypeId, CancellationToken cancellationToken) =>
        catalog.ListBrandsAsync(vehicleTypeId, cancellationToken);

    [HttpGet("brands/{vehicleBrandId:guid}/models")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<VehicleModelResponse>> Models(Guid vehicleBrandId, CancellationToken cancellationToken) =>
        catalog.ListModelsAsync(vehicleBrandId, cancellationToken);

    [HttpPost("types")]
    [Authorize(Roles = "Admin")]
    public Task<VehicleTypeResponse> CreateType(CreateVehicleTypeRequest request, CancellationToken cancellationToken) =>
        catalog.CreateTypeAsync(request, cancellationToken);

    [HttpPut("types/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public Task<VehicleTypeResponse> UpdateType(Guid id, UpdateVehicleTypeRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateTypeAsync(id, request, cancellationToken);

    [HttpPost("brands")]
    [Authorize(Roles = "Admin")]
    public Task<VehicleBrandResponse> CreateBrand(CreateVehicleBrandRequest request, CancellationToken cancellationToken) =>
        catalog.CreateBrandAsync(request, cancellationToken);

    [HttpPut("brands/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public Task<VehicleBrandResponse> UpdateBrand(Guid id, UpdateVehicleBrandRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateBrandAsync(id, request, cancellationToken);

    [HttpPost("models")]
    [Authorize(Roles = "Admin")]
    public Task<VehicleModelResponse> CreateModel(CreateVehicleModelRequest request, CancellationToken cancellationToken) =>
        catalog.CreateModelAsync(request, cancellationToken);

    [HttpPut("models/{id:guid}")]
    [Authorize(Roles = "Admin")]
    public Task<VehicleModelResponse> UpdateModel(Guid id, UpdateVehicleModelRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateModelAsync(id, request, cancellationToken);
}
