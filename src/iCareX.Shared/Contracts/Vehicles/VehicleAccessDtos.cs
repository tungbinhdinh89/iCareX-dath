using System.Text.Json.Serialization;

namespace iCareX.Shared.Contracts.Vehicles;

public sealed record VehiclePermissionGrant(string Code, string Scope);

public sealed record VehiclePermissionsResponse(
    Guid VehicleId, string Role, string AccessType, bool IsArchived,
    IReadOnlyCollection<VehiclePermissionGrant> Permissions);

public sealed record CurrentUserPermissionsResponse(
    Guid UserId, IReadOnlyCollection<string> GlobalRoles,
    IReadOnlyCollection<VehiclePermissionsResponse> Vehicles);

public sealed record VehicleMemberResponse(
    Guid UserId, string Name, string Role, DateTimeOffset JoinedAtUtc, long Version);

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record UpdateVehicleMemberRequest(string Role, long Version);
