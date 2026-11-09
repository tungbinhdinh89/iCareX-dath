using iCareX.Shared.Contracts.Identity;

namespace iCareX.Application.Features.Identity.Contracts;

public interface IAccountSecurityService
{
    Task SendVerificationAsync(string email, CancellationToken ct);
    Task VerifyEmailAsync(VerifyEmailRequest request, CancellationToken ct);
    Task ForgotPasswordAsync(string email, CancellationToken ct);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
    Task ChangePasswordAsync(Guid actorId, ChangePasswordRequest request, CancellationToken ct);
}

public interface IAccountEmailSender
{
    void EnsureConfigured();
    Task SendAsync(string recipient, string subject, string text, CancellationToken ct);
}

public sealed class EmailDeliveryUnavailableException : Exception;
