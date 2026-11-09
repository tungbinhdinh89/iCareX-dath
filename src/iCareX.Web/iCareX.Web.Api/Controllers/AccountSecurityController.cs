using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Shared.Contracts.Identity;
using iCareX.Web.Api.Authorization;

namespace iCareX.Web.Api.Controllers;

[ApiController, Route("api/v1/auth")]
[ServiceFilter(typeof(VehicleCsrfFilter))]
[EnableRateLimiting("auth-actions")]
public sealed class AccountSecurityController(IAccountSecurityService security) : ControllerBase
{
    [HttpPost("verification-email")]
    public async Task<IActionResult> SendVerification(EmailActionRequest request, CancellationToken ct)
    {
        await security.SendVerificationAsync(request.Email, ct);
        return Accepted(new AccountActionAcceptedResponse("Nếu email phù hợp, bạn sẽ nhận được liên kết xác thực."));
    }
    [HttpPost("verify-email")]
    public async Task<IActionResult> Verify(VerifyEmailRequest request, CancellationToken ct)
    {
        await security.VerifyEmailAsync(request, ct);
        return NoContent();
    }
    [HttpPost("forgot-password")]
    public async Task<IActionResult> Forgot(EmailActionRequest request, CancellationToken ct)
    {
        await security.ForgotPasswordAsync(request.Email, ct);
        return Accepted(new AccountActionAcceptedResponse("Nếu email đã được đăng ký, bạn sẽ nhận được liên kết đặt lại mật khẩu."));
    }
    [HttpPost("reset-password")]
    public async Task<IActionResult> Reset(ResetPasswordRequest request, CancellationToken ct)
    {
        await security.ResetPasswordAsync(request, ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
    [HttpPost("change-password"), Authorize(Policy = "VerifiedAccount")]
    public async Task<IActionResult> Change(ChangePasswordRequest request, CancellationToken ct)
    {
        await security.ChangePasswordAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request, ct);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }
}
