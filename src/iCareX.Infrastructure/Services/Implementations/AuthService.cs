using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using iCareX.Application.Abstractions;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Shared.Contracts.Identity;
using iCareX.Domain.Entities;
using iCareX.Domain.Common;
using iCareX.Infrastructure.Persistence;

namespace iCareX.Infrastructure.Services.Implementations;

public sealed class AuthService(
    IAuthRepository repository,
    GarageDbContext db,
    IAccountSecurityService security,
    IAccountEmailSender emailSender,
    UserManager<ApplicationUser> users,
    RoleManager<IdentityRole<Guid>> roles,
    IDataProtectionProvider protectionProvider,
    IBusinessClock clock,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AccountResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Registration requested for {Email}", MaskEmail(request.Email?.Trim() ?? ""));
        ValidateRequest(request);
        emailSender.EnsureConfigured();
        var email = request.Email?.Trim() ?? "";
        if (await users.FindByEmailAsync(email) is not null)
        {
            logger.LogWarning("Registration rejected because email already exists: {Email}", MaskEmail(email));
            throw new AuthConflictException("Email đã được đăng ký.");
        }

        var account = new Account { Name = request.Name.Trim(), Phone = request.Phone?.Trim() ?? "" };
        var user = new ApplicationUser
        {
            Id = account.Id, Email = email, UserName = email, EmailConfirmed = false
        };
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            logger.LogWarning("Registration validation failed for {Email}", MaskEmail(email));
            throw new AuthValidationException(result.Errors.Select(error => error.Description).ToArray());
        }
        if (!await roles.RoleExistsAsync("User"))
        {
            var createRoleResult = await roles.CreateAsync(new IdentityRole<Guid>("User"));
            if (!createRoleResult.Succeeded)
                throw new AuthValidationException(
                    createRoleResult.Errors.Select(error => error.Description).ToArray());
        }
        var roleResult = await users.AddToRoleAsync(user, "User");
        if (!roleResult.Succeeded)
            throw new AuthValidationException(roleResult.Errors.Select(error => error.Description).ToArray());
        repository.AddAccount(account);
        repository.AddSettings(new UserSettings { AccountId = account.Id });
        await repository.SaveChangesAsync(cancellationToken);
        await security.SendVerificationAsync(email, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogInformation("Registration succeeded for account {AccountId} ({Email})", account.Id, MaskEmail(email));
        return ToResponse(account, user, ["User"]);
    }

    public async Task<(AccountResponse Account, Guid SessionId)?> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            logger.LogWarning("Login rejected because required credentials were missing");
            return null;
        }
        logger.LogInformation("Login requested for {Email}", MaskEmail(email));
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            logger.LogWarning("Login failed for {Email}: user missing or locked", MaskEmail(email));
            return null;
        }
        await using var transaction = await repository.BeginTransactionAsync(cancellationToken);
        await db.Users.FromSqlInterpolated($"SELECT * FROM \"AspNetUsers\" WHERE \"Id\" = {user.Id} FOR UPDATE").AsNoTracking().SingleAsync(cancellationToken);
        await db.Entry(user).ReloadAsync(cancellationToken);
        if (await users.IsLockedOutAsync(user)) return null;
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning("Login failed for {Email}: invalid password", MaskEmail(email));
            return null;
        }
        await users.ResetAccessFailedCountAsync(user);
        var account = await repository.FindAccountAsync(user.Id, cancellationToken);
        if (account is null)
        {
            logger.LogWarning("Login failed because account profile was missing for {UserId}", user.Id);
            return null;
        }
        var sessionId = Guid.NewGuid();
        repository.AddSession(
            account.Id,
            sessionId,
            protectionProvider.CreateProtector("iCareX.AuthSession").Protect(sessionId.ToString("N")),
            clock.UtcNow,
            clock.UtcNow.AddHours(8));
        await repository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Login succeeded for account {AccountId} ({Email})", account.Id, MaskEmail(email));
        await transaction.CommitAsync(cancellationToken);
        return (ToResponse(account, user, await users.GetRolesAsync(user)), sessionId);
    }

    public async Task<AccountResponse?> GetAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var account = await repository.FindAccountAsync(accountId, cancellationToken);
        var user = await users.FindByIdAsync(accountId.ToString());
        if (account is null || user is null)
            return null;
        return ToResponse(account, user, await users.GetRolesAsync(user));
    }

    public async Task RevokeSessionAsync(Guid accountId, Guid sessionId, CancellationToken cancellationToken)
    {
        if (!await repository.RevokeSessionAsync(accountId, sessionId, clock.UtcNow, cancellationToken))
            return;
        await repository.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRequest(RegisterRequest request)
    {
        var name = request.Name?.Trim();
        var email = request.Email?.Trim();
        var validEmail = false;
        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                validEmail = new System.Net.Mail.MailAddress(email).Address.Equals(
                    email, StringComparison.OrdinalIgnoreCase);
            }
            catch (FormatException) { validEmail = false; }
        }
        if (name is not { Length: >= 2 and <= 60 } || !validEmail)
            throw new AuthValidationException(["Tên hoặc email không hợp lệ."]);
        var phone = request.Phone?.Trim();
        if (!string.IsNullOrEmpty(phone) &&
            !System.Text.RegularExpressions.Regex.IsMatch(phone, "^0\\d{9}$"))
            throw new AuthValidationException(["Số điện thoại không hợp lệ."]);
    }

    private AccountResponse ToResponse(Account account, ApplicationUser user, IEnumerable<string> roles) =>
        new(account.Id, account.Name, user.Email ?? "", account.Phone, roles.ToArray(),
            user.LockoutEnabled && user.LockoutEnd > clock.UtcNow ? "Locked" : user.EmailConfirmed ? "Active" : "PendingVerification",
            user.EmailConfirmed);

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***";
        return $"{email[0]}***{email[(at - 1)..]}";
    }
}
