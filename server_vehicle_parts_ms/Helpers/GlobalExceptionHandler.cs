using Microsoft.AspNetCore.Diagnostics;

namespace server_vehicle_parts_ms.Helpers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception,
            "Unhandled exception on {Method} {Path} - {Message}",
            httpContext.Request.Method, httpContext.Request.Path, exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            Success = false,
            Message = "Internal server error"
        }, cancellationToken);

        return true;
    }
}
