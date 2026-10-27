using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartHaat.Application.Catalog;
using SmartHaat.Domain.Orders;

namespace SmartHaat.Api.Infrastructure;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            CatalogValidationException => (StatusCodes.Status400BadRequest, "Request validation failed"),
            ArgumentException or ArgumentOutOfRangeException => (StatusCodes.Status400BadRequest, "Request validation failed"),
            CatalogConflictException => (StatusCodes.Status409Conflict, "A catalog value conflicts with an existing record"),
            OrderStateException => (StatusCodes.Status409Conflict, "The requested state change is not allowed"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };
        context.Response.StatusCode = status;
        var details = new ProblemDetails { Status = status, Title = title, Detail = status < 500 ? exception.Message : null, Instance = context.Request.Path };
        details.Extensions["traceId"] = context.TraceIdentifier;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext { HttpContext = context, ProblemDetails = details, Exception = exception });
    }
}
