using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using iCareX.Application.Abstractions;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Domain.Entities;
using iCareX.Infrastructure.Persistence;
using iCareX.Shared.Contracts.Identity;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class AccountSecurityService(GarageDbContext db, UserManager<ApplicationUser> users,
    IAccountEmailSender email, IOptions<AccountEmailOptions> options, IBusinessClock clock) : IAccountSecurityService
{
    private const string Provider = "iCareX.AccountActions";
    private const string Verification = "EmailVerification";
    private const string Reset = "PasswordReset";

    public Task SendVerificationAsync(string address, CancellationToken ct) => SendAsync(address, false, ct);
    public Task ForgotPasswordAsync(string address, CancellationToken ct) => SendAsync(address, true, ct);

    private async Task SendAsync(string address, bool reset, CancellationToken ct)
    {
        email.EnsureConfigured();
        ValidateEmail(address);
        var user = await users.FindByEmailAsync(address.Trim());
        // Same successful response for unknown addresses and already verified accounts.
        if (user is null || !reset && user.EmailConfirmed) return;
        // Registration already owns a transaction; sending failure must roll it back too.
        await using var transaction = db.Database.CurrentTransaction is null ? await db.Database.BeginTransactionAsync(ct) : null;
        await LockUser(user, ct);
        if (!reset && user.EmailConfirmed) return;
        var token = reset ? await users.GeneratePasswordResetTokenAsync(user) : await users.GenerateEmailConfirmationTokenAsync(user);
        Check(await users.SetAuthenticationTokenAsync(user, Provider, reset ? Reset : Verification, Hash(token)));
        var link = options.Value.Link(reset ? "dat-lai-mat-khau" : "xac-thuc-email",
            $"email={Uri.EscapeDataString(user.Email!)}&token={Uri.EscapeDataString(token)}");
        await email.SendAsync(user.Email!, reset ? "iCareX - Đặt lại mật khẩu" : "iCareX - Xác thực email",
            $"Mở liên kết sau để {(reset ? "đặt lại mật khẩu" : "xác thực email")}:\n{link}\nLiên kết có hiệu lực trong 1 giờ và chỉ dùng một lần. Nếu bạn không yêu cầu, hãy bỏ qua email này.", ct);
        if (transaction is not null) await transaction.CommitAsync(ct);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct)
    {
        var user = await Find(request.Email, request.Token);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockUser(user, ct);
        if (user.EmailConfirmed || !await Matches(user, Verification, request.Token)) InvalidToken();
        Check(await users.ConfirmEmailAsync(user, request.Token));
        Check(await users.RemoveAuthenticationTokenAsync(user, Provider, Verification));
        Audit(user.Id, "auth.email.verified");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await Find(request.Email, request.Token);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockUser(user, ct);
        if (!await Matches(user, Reset, request.Token)) InvalidToken();
        Check(await users.ResetPasswordAsync(user, request.Token, request.NewPassword));
        Check(await users.RemoveAuthenticationTokenAsync(user, Provider, Reset));
        await RevokeSessions(user.Id, ct);
        Audit(user.Id, "auth.password.reset");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task ChangePasswordAsync(Guid actorId, ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(actorId.ToString()) ?? throw new AuthValidationException(["Tài khoản không hợp lệ."]);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockUser(user, ct);
        Check(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword));
        Check(await users.RemoveAuthenticationTokenAsync(user, Provider, Reset));
        await RevokeSessions(actorId, ct);
        Audit(actorId, "auth.password.changed");
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private Task<int> RevokeSessions(Guid actorId, CancellationToken ct) => db.Sessions
        .Where(x => x.AccountId == actorId && x.RevokedAtUtc == null)
        .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAtUtc, clock.UtcNow), ct);

    private async Task LockUser(ApplicationUser user, CancellationToken ct)
    {
        await db.Users.FromSqlInterpolated($"SELECT * FROM \"AspNetUsers\" WHERE \"Id\" = {user.Id} FOR UPDATE").AsNoTracking().SingleAsync(ct);
        await db.Entry(user).ReloadAsync(ct);
    }

    private async Task<ApplicationUser> Find(string address, string token)
    {
        ValidateEmail(address);
        if (string.IsNullOrEmpty(token) || token.Length > 4096) InvalidToken();
        return await users.FindByEmailAsync(address.Trim()) ?? throw new AuthValidationException(["Liên kết không hợp lệ hoặc đã hết hạn."]);
    }

    private async Task<bool> Matches(ApplicationUser user, string purpose, string token)
    {
        var stored = await users.GetAuthenticationTokenAsync(user, Provider, purpose);
        return stored is not null && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(stored), Encoding.ASCII.GetBytes(Hash(token)));
    }

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static void InvalidToken() => throw new AuthValidationException(["Liên kết không hợp lệ hoặc đã hết hạn."]);
    private static void Check(IdentityResult result)
    {
        if (!result.Succeeded) throw new AuthValidationException(result.Errors.Select(x => x.Description).ToArray());
    }
    private static void ValidateEmail(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length > 320 || !System.Net.Mail.MailAddress.TryCreate(address.Trim(), out var parsed)
            || !string.Equals(parsed.Address, address.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new AuthValidationException(["Email không hợp lệ."]);
    }
    private void Audit(Guid actor, string action) => db.AuditLogs.Add(new AuditLog
    {
        ActorUserId = actor, Action = action, EntityType = nameof(Account), EntityId = actor, CreatedAtUtc = clock.UtcNow
    });
}
