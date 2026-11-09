using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Application.Features.Authorization.Contracts;

public interface IVehicleInvitationService
{
    Task<IReadOnlyCollection<VehicleInvitationResponse>> ListAsync(Guid actor, Guid vehicleId, CancellationToken ct);
    Task<VehicleInvitationResponse> CreateAsync(Guid actor, Guid vehicleId, CreateVehicleInvitationRequest request, CancellationToken ct);
    Task RevokeAsync(Guid actor, Guid vehicleId, Guid invitationId, long version, CancellationToken ct);
    Task<VehicleInvitationResponse> PreviewAsync(Guid invitationId, string token, CancellationToken ct);
    Task<IReadOnlyCollection<VehicleInvitationResponse>> InboxAsync(Guid actor, CancellationToken ct);
    Task RespondAsync(Guid actor, Guid invitationId, string token, bool accept, CancellationToken ct);
}

public sealed class InvitationExpiredException : Exception;
