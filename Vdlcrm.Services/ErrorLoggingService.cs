using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Vdlcrm.Services;

public class ErrorLoggingService
{
    private readonly ILogger<ErrorLoggingService> _logger;

    public ErrorLoggingService(ILogger<ErrorLoggingService> logger)
    {
        _logger = logger;
    }

    public async Task LogExceptionAsync(Exception ex, string requestPath)
    {
        _logger.LogError(ex, "Exception captured by ErrorLoggingService for path: {Path}", requestPath);
        await Task.CompletedTask;
    }
}