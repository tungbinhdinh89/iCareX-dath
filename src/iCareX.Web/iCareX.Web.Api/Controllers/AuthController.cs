using System.Security.Claims;
using System.Security.Cryptography;
using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using iCareX.Application.Features.Identity.Contracts;
using iCareX.Shared.Contracts.Identity;

namespace iCareX.Web.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    IAuthService auth,
    IDataProtectionProvider protectionProvider) : ControllerBase
{
    private const string CsrfCookie = "icarex-csrf";
    private const string CsrfHeader = "X-CSRF-TOKEN";

    [HttpGet("csrf")]
    public ActionResult<CsrfResponse> Csrf()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var protectedToken = protectionProvider.CreateProtector("iCareX.Csrf").Protect(token);
        Response.Cookies.Append(CsrfCookie, protectedToken, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps || Request.Host.Host is not ("localhost" or "127.0.0.1")
        });
        return Ok(new CsrfResponse(token));
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-actions")]
    public async Task<ActionResult<RegistrationResponse>> Register(
        [FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!IsCsrfValid())
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["csrf"] = ["CSRF token không hợp lệ."]
            }));
        try
        {
            var result = await auth.RegisterAsync(request, cancellationToken);
            return Created("api/v1/auth/me", result);
        }
        catch (AuthValidationException exception)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["registration"] = exception.Errors.ToArray()
            }));
        }
        catch (AuthConflictException exception)
        {
            return Conflict(new ProblemDetails { Title = "Email đã tồn tại.", Detail = exception.Message });
        }
        catch (DbException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Database unavailable.",
                    Detail = "Không thể kết nối database. Kiểm tra User Secret ConnectionStrings:iCareX."
                });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-actions")]
    public async Task<ActionResult<AccountResponse>> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (!IsCsrfValid())
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["csrf"] = ["CSRF token không hợp lệ."]
            }));
        (AccountResponse Account, Guid SessionId)? result;
        try
        {
            result = await auth.LoginAsync(request, cancellationToken);
        }
        catch (DbException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Title = "Database unavailable.",
                    Detail = "Không thể kết nối database. Kiểm tra User Secret ConnectionStrings:iCareX."
                });
        }
        if (result is null)
            return Unauthorized(new ProblemDetails { Title = "Đăng nhập thất bại.", Detail = "Email hoặc mật khẩu không đúng." });
        await SignIn(result.Value.Account, result.Value.SessionId);
        return Ok(result.Value.Account);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AccountResponse>> Me(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId))
            return Unauthorized();
        var account = await auth.GetAccountAsync(accountId, cancellationToken);
        return account is null ? Unauthorized() : Ok(account);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        if (!IsCsrfValid())
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["csrf"] = ["CSRF token không hợp lệ."]
            }));
        if (Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var accountId) &&
            Guid.TryParse(User.FindFirstValue("sid"), out var sessionId))
            await auth.RevokeSessionAsync(accountId, sessionId, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    private bool IsCsrfValid()
    {
        if (!Request.Headers.TryGetValue(CsrfHeader, out var header) ||
            !Request.Cookies.TryGetValue(CsrfCookie, out var cookie) || header.Count != 1)
            return false;
        try
        {
            var token = protectionProvider.CreateProtector("iCareX.Csrf").Unprotect(cookie);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(token), Convert.FromBase64String(header[0]!));
        }
        catch (FormatException) { return false; }
        catch (CryptographicException) { return false; }
    }

    private Task SignIn(AccountResponse account, Guid sessionId)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Email, account.Email));
        identity.AddClaim(new Claim("sid", sessionId.ToString()));
        identity.AddClaims(account.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = false, AllowRefresh = false });
    }
}
