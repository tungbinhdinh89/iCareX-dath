using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using iCareX.Application.Abstractions;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Domain.Authorization;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;
using iCareX.Shared.Contracts.Vehicles;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class VehicleMemberService(
    GarageDbContext db, IVehicleAccessService access, IBusinessClock clock) : IVehicleMemberService
{
    public async Task<IReadOnlyCollection<VehicleMemberResponse>> ListAsync(Guid actorId, Guid vehicleId, CancellationToken cancellationToken)
    {
        await access.RequireAsync(actorId, vehicleId, VehiclePermissions.MemberRead, cancellationToken);
        return await (from member in db.VehicleMembers.AsNoTracking()
                      join account in db.Accounts.AsNoTracking() on member.UserId equals account.Id
                      where member.VehicleId == vehicleId && member.RevokedAtUtc == null
                      orderby member.JoinedAtUtc, member.UserId
                      select new VehicleMemberResponse(member.UserId, account.Name, member.Role.ToString(),
                          member.JoinedAtUtc, member.Version)).ToArrayAsync(cancellationToken);
    }

    public async Task<VehicleMemberResponse> UpdateAsync(Guid actorId, Guid vehicleId, Guid memberUserId,
        UpdateVehicleMemberRequest request, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        await access.RequireAsync(actorId, vehicleId, VehiclePermissions.MemberUpdate, cancellationToken);
        if (!Enum.TryParse<VehicleMemberRole>(request.Role, false, out var role)
            || !Enum.IsDefined(role) || role == VehicleMemberRole.Owner
            || request.Role != role.ToString())
            throw new VehicleValidationException(["Vai trò thành viên phải là Manager, Contributor hoặc Viewer."]);
        var member = await MutableMember(vehicleId, memberUserId, request.Version, cancellationToken);
        var oldRole = member.Role;
        member.Role = role;
        member.Version++;
        Audit(actorId, member, "vehicle.member.updated", oldRole.ToString(), role.ToString());
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        var name = await db.Accounts.Where(x => x.Id == member.UserId).Select(x => x.Name).SingleAsync(cancellationToken);
        return new(member.UserId, name, member.Role.ToString(), member.JoinedAtUtc, member.Version);
    }

    public async Task RevokeAsync(Guid actorId, Guid vehicleId, Guid memberUserId, long version, CancellationToken cancellationToken)
    {
        await using var transaction = await VehicleWriteLock.BeginAsync(db, vehicleId, cancellationToken);
        await access.RequireAsync(actorId, vehicleId, VehiclePermissions.MemberRemove, cancellationToken);
        var member = await MutableMember(vehicleId, memberUserId, version, cancellationToken);
        member.RevokedAtUtc = clock.UtcNow;
        member.Version++;
        Audit(actorId, member, "vehicle.member.revoked", member.Role.ToString(), null);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<VehicleMember> MutableMember(Guid vehicleId, Guid userId, long version, CancellationToken cancellationToken)
    {
        var member = await db.VehicleMembers.SingleOrDefaultAsync(
            x => x.VehicleId == vehicleId && x.UserId == userId && x.RevokedAtUtc == null, cancellationToken)
            ?? throw new VehicleNotFoundException();
        if (member.Role == VehicleMemberRole.Owner)
            throw new VehicleConflictException("Không thể sửa hoặc thu hồi Owner qua API thành viên. Cần chuyển quyền sở hữu.");
        if (member.Version != version || version < 0)
            throw new VehicleConflictException("Phiên bản thành viên đã thay đổi. Vui lòng tải lại.");
        if (await db.VehicleMembers.CountAsync(x => x.VehicleId == vehicleId
                && x.Role == VehicleMemberRole.Owner && x.RevokedAtUtc == null, cancellationToken) != 1)
            throw new VehicleConflictException("Phương tiện phải có đúng một Owner đang hoạt động.");
        return member;
    }

    private void Audit(Guid actor, VehicleMember member, string action, string? oldRole, string? newRole) =>
        db.AuditLogs.Add(new AuditLog
        {
            ActorUserId = actor, VehicleId = member.VehicleId, Action = action,
            EntityType = nameof(VehicleMember), EntityId = member.Id, CreatedAtUtc = clock.UtcNow,
            OldValues = JsonSerializer.Serialize(new { userId = member.UserId, role = oldRole }),
            NewValues = JsonSerializer.Serialize(new { userId = member.UserId, role = newRole, revokedAtUtc = member.RevokedAtUtc })
        });
}
