using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Vdlcrm.Services;

public class AutoPunchOutBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoPunchOutBackgroundService> _logger;

    public AutoPunchOutBackgroundService(IServiceProvider serviceProvider, ILogger<AutoPunchOutBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Auto Punch Out Background Service has started.");

        // Wait for 1 minute before starting the first cycle to allow the app to start up smoothly.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        // Set the last run time to now to prevent it from running immediately on the first check.
        // The first real run will happen after the configured interval.
        DateTime lastRunTime = DateTime.UtcNow;
 
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var settingsService = scope.ServiceProvider.GetRequiredService<SettingsService>();
                var attendanceService = scope.ServiceProvider.GetRequiredService<AttendanceService>();

                // Check configured interval
                var intervalStr = await settingsService.GetSettingValueAsync("AutoPunchOutWorkerIntervalHours", "1");
                if (!double.TryParse(intervalStr, out double intervalHours)) intervalHours = 1; // Default to 1 hour for easier testing

                // Check if the configured interval has passed since the last run
                if ((DateTime.UtcNow - lastRunTime).TotalHours >= intervalHours)
                {
                    var isEnabledStr = await settingsService.GetSettingValueAsync("AutoPunchOutWorkerEnabled", "true");
                    bool.TryParse(isEnabledStr, out bool isEnabled);

                    if (isEnabled)
                    {
                        var mode = await settingsService.GetSettingValueAsync("AutoPunchOutWorkerMode", "Day");
                        var startStr = await settingsService.GetSettingValueAsync("AutoPunchOutDayStart", "08:00");
                        var endStr = await settingsService.GetSettingValueAsync("AutoPunchOutDayEnd", "20:00");

                        // Get Local Indian Time (IST) for Day/Night checking
                        var currentIstTime = DateTime.UtcNow.AddHours(5).AddMinutes(30);
                        var timeOfDay = currentIstTime.TimeOfDay;

                        TimeSpan.TryParse(startStr, out TimeSpan startTime);
                        TimeSpan.TryParse(endStr, out TimeSpan endTime);
                        if (startTime == default) startTime = new TimeSpan(8, 0, 0); // Default 8 AM
                        if (endTime == default) endTime = new TimeSpan(20, 0, 0); // Default 8 PM

                        bool shouldRun = mode.Equals("Both", StringComparison.OrdinalIgnoreCase) || 
                                         (mode.Equals("Day", StringComparison.OrdinalIgnoreCase) && timeOfDay >= startTime && timeOfDay <= endTime) ||
                                         (mode.Equals("Night", StringComparison.OrdinalIgnoreCase) && (timeOfDay >= endTime || timeOfDay <= startTime));

                        if (shouldRun)
                        {
                            _logger.LogInformation($"[{currentIstTime:HH:mm}] Starting Auto Punch Out Process. Mode: {mode}");
                            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                            await attendanceService.ProcessAutoPunchOutsAsync();

                            stopwatch.Stop();
                            _logger.LogInformation($"Auto Punch Out Process finished in {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
                        }
                        else
                        {
                            _logger.LogInformation($"[{currentIstTime:HH:mm}] Auto Punch Out skipped. Current time is outside the configured '{mode}' mode window ({startStr} - {endStr}).");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Auto Punch Out worker is currently DISABLED by Admin.");
                    }

                    // Update last run time regardless of whether it ran, to reset the timer
                    lastRunTime = DateTime.UtcNow;
                }

                // Wait for 5 minutes before the next check. This prevents a tight loop.
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in Auto Punch Out Background Service.");
                // In case of an error, wait longer before retrying to avoid spamming logs.
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }
        }
    }
}