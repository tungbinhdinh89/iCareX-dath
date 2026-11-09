using iCareX.Web.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Web.Api.Controllers;

[ApiController]
[Route("api/v1/vehicle-catalog")]
[ServiceFilter(typeof(VehicleCsrfFilter))]
public sealed class VehicleCatalogController(IVehicleCatalogService catalog) : ControllerBase
{
    [HttpGet("types")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<VehicleTypeResponse>> Types(CancellationToken cancellationToken) =>
        catalog.ListTypesAsync(cancellationToken);

    [HttpGet("types/{vehicleType}/brands")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<VehicleBrandResponse>> Brands(string vehicleType, CancellationToken cancellationToken) =>
        catalog.ListBrandsAsync(vehicleType, cancellationToken);

    [HttpGet("brands/{vehicleBrandId:guid}/models")]
    [AllowAnonymous]
    public Task<IReadOnlyCollection<VehicleModelResponse>> Models(Guid vehicleBrandId, CancellationToken cancellationToken) =>
        catalog.ListModelsAsync(vehicleBrandId, cancellationToken);

    [HttpPost("brands")]
    [Authorize(Roles = "SystemAdmin,Admin", Policy = "VerifiedAccount")]
    public Task<VehicleBrandResponse> CreateBrand(CreateVehicleBrandRequest request, CancellationToken cancellationToken) =>
        catalog.CreateBrandAsync(request, cancellationToken);

    [HttpPut("brands/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,Admin", Policy = "VerifiedAccount")]
    public Task<VehicleBrandResponse> UpdateBrand(Guid id, UpdateVehicleBrandRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateBrandAsync(id, request, cancellationToken);

    [HttpPost("models")]
    [Authorize(Roles = "SystemAdmin,Admin", Policy = "VerifiedAccount")]
    public Task<VehicleModelResponse> CreateModel(CreateVehicleModelRequest request, CancellationToken cancellationToken) =>
        catalog.CreateModelAsync(request, cancellationToken);

    [HttpPut("models/{id:guid}")]
    [Authorize(Roles = "SystemAdmin,Admin", Policy = "VerifiedAccount")]
    public Task<VehicleModelResponse> UpdateModel(Guid id, UpdateVehicleModelRequest request, CancellationToken cancellationToken) =>
        catalog.UpdateModelAsync(id, request, cancellationToken);
}
