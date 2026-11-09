using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace iCareX.Web.Api.Authorization;

public sealed class VehicleCsrfFilter(IDataProtectionProvider protection) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)) { await next(); return; }
        var valid = false;
        if (request.Cookies.TryGetValue("icarex-csrf", out var cookie)
            && request.Headers.TryGetValue("X-CSRF-TOKEN", out var header) && header.Count == 1)
        {
            try
            {
                valid = CryptographicOperations.FixedTimeEquals(
                    Convert.FromBase64String(protection.CreateProtector("iCareX.Csrf").Unprotect(cookie)),
                    Convert.FromBase64String(header[0]!));
            }
            catch (FormatException) { }
            catch (CryptographicException) { }
        }
        if (!valid) { context.Result = new BadRequestObjectResult(new ProblemDetails { Status = 400, Title = "Invalid CSRF token." }); return; }
        await next();
    }
}
