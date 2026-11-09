using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Vehicles.Contracts;
using iCareX.Application.Features.Identity.Contracts;

namespace iCareX.Web.Api.Authorization;

public sealed class VehicleApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var status = context.Exception switch
        {
            VehicleNotFoundException => 404,
            VehicleAccessDeniedException => 403,
            VehicleConflictException => 409,
            VehicleValidationException => 400,
            AuthValidationException => 400,
            EmailDeliveryUnavailableException => 503,
            InvitationExpiredException => 410,
            _ => 0
        };
        if (status == 0) return;
        if (context.Exception is AuthValidationException validation)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]> { ["account"] = validation.Errors.ToArray() }));
            context.ExceptionHandled = true;
            return;
        }
        if (context.Exception is VehicleValidationException vehicleValidation)
        {
            context.Result = new BadRequestObjectResult(new ValidationProblemDetails(new Dictionary<string, string[]> { ["vehicle"] = vehicleValidation.Errors.ToArray() }));
            context.ExceptionHandled = true;
            return;
        }
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status, Title = status switch { 404 => "Vehicle not found.", 403 => "Vehicle access denied.", 409 => "Membership conflict.", 410 => "Lời mời đã hết hạn.", 503 => "Chưa thể gửi email. Vui lòng thử lại sau.", _ => "Invalid membership request." },
            Detail = status is 400 or 409 ? context.Exception.Message : null
        }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
