using iCareX.Domain.Entities;
using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Application.Features.Authorization.Contracts;

public sealed record VehicleMembership(
    Guid VehicleId, Guid UserId, Guid OwnerAccountId, VehicleMemberRole Role, bool IsArchived);

// References only. Creator/assignee are resolved by the server, never accepted from request DTOs.
public sealed record VehicleResourceReference(Guid VehicleId, string Kind, Guid Id);

public interface IVehicleMembershipResolver
{
    Task<VehicleMembership?> ResolveAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken);
}

public interface IVehicleAccessService
{
    Task<VehicleMembership> RequireAsync(Guid userId, Guid vehicleId, string permission, CancellationToken cancellationToken);
    Task<bool> CanAccessResourceAsync(Guid userId, VehicleResourceReference resource, string permission, CancellationToken cancellationToken);
    Task<VehiclePermissionsResponse> GetPermissionsAsync(Guid userId, Guid vehicleId, CancellationToken cancellationToken);
    Task<CurrentUserPermissionsResponse> GetCurrentPermissionsAsync(Guid userId, CancellationToken cancellationToken);
}

public interface IVehicleMemberService
{
    Task<IReadOnlyCollection<VehicleMemberResponse>> ListAsync(Guid actorId, Guid vehicleId, CancellationToken cancellationToken);
    Task<VehicleMemberResponse> UpdateAsync(Guid actorId, Guid vehicleId, Guid memberUserId, UpdateVehicleMemberRequest request, CancellationToken cancellationToken);
    Task RevokeAsync(Guid actorId, Guid vehicleId, Guid memberUserId, long version, CancellationToken cancellationToken);
}

public sealed class VehicleAccessDeniedException : Exception;
