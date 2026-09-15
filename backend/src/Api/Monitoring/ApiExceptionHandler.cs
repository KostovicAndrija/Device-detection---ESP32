using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Monitoring;

public sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            UnauthorizedAccessException => StatusCodes.Status403Forbidden,
            Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => StatusCodes.Status409Conflict,
            Microsoft.EntityFrameworkCore.DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" } } => StatusCodes.Status409Conflict,
            ArgumentException => StatusCodes.Status400BadRequest,
            InvalidOperationException => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled API exception.");
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = status == 500 ? "An unexpected error occurred." :
                    exception is Microsoft.EntityFrameworkCore.DbUpdateException ? "Podaci su u međuvremenu promenjeni ili već postoje. Osvežite prikaz i pokušajte ponovo." : exception.Message
            },
            Exception = exception
        });
    }
}
