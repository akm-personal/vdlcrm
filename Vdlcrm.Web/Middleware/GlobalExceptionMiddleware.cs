using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ErrorLoggingService errorLoggingService)
    {
        try
        {
            // Proceed to the next middleware/controller
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log error locally in console/files
            _logger.LogError(ex, "An unhandled exception occurred in the application.");
            
            // Log ANY exception directly to the ExceptionHistory database table
            await errorLoggingService.LogExceptionAsync(ex, context);

            // Return a graceful error response to the client
            await HandleExceptionAsync(context);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var result = JsonSerializer.Serialize(new
        {
            success = false,
            message = "An unexpected error occurred. Please provide the Trace ID to support.",
            traceId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier
        });

        return context.Response.WriteAsync(result);
    }
}