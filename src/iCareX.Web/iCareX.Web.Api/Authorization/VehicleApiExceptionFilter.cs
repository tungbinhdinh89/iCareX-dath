using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using iCareX.Application.Features.Authorization.Contracts;
using iCareX.Application.Features.Vehicles.Contracts;

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
            _ => 0
        };
        if (status == 0) return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status, Title = status switch { 404 => "Vehicle not found.", 403 => "Vehicle access denied.", 409 => "Membership conflict.", _ => "Invalid membership request." },
            Detail = status is 400 or 409 ? context.Exception.Message : null
        }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
