using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class AttendanceService
{
    private readonly AppDbContext _context;
    private readonly SettingsService _settingsService;

    public AttendanceService(AppDbContext context, SettingsService settingsService)
    {
        _context = context;
        _settingsService = settingsService;
    }

    // Haversine formula to calculate distance between two coordinates in meters
    private double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3; // Earth radius in meters
        var rLat1 = lat1 * Math.PI / 180;
        var rLat2 = lat2 * Math.PI / 180;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(rLat1) * Math.Cos(rLat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    public async Task ValidateRadiusAsync(double lat, double lon)
    {
        var libLat = double.Parse(await _settingsService.GetSettingValueAsync("LibraryLatitude", "0"));
        var libLon = double.Parse(await _settingsService.GetSettingValueAsync("LibraryLongitude", "0"));
        var allowedRadius = double.Parse(await _settingsService.GetSettingValueAsync("AttendanceRadius", "50"));

        var distance = CalculateDistanceMeters(lat, lon, libLat, libLon);
        
        if (distance > allowedRadius)
        {
            throw new InvalidOperationException($"You are {Math.Round(distance)} meters away. You must be within {allowedRadius} meters of the library to punch in/out.");
        }
    }

    public async Task<AttendanceRecord> PunchInAsync(string vdlId, double lat, double lon, int? shiftId = null)
    {
        await ValidateRadiusAsync(lat, lon);

        // Check if already punched in without punch out
        var activeRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.VdlId == vdlId && a.PunchOutTime == null);

        if (activeRecord != null)
        {
            throw new InvalidOperationException("You are already punched in.");
        }

        var record = new AttendanceRecord
        {
            VdlId = vdlId,
            ShiftId = shiftId,
            PunchInTime = DateTime.UtcNow,
            PunchInLatitude = lat,
            PunchInLongitude = lon
        };

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();
        return record;
    }

    public async Task<AttendanceRecord> PunchOutAsync(string vdlId, double lat, double lon)
    {
        await ValidateRadiusAsync(lat, lon);

        var activeRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(a => a.VdlId == vdlId && a.PunchOutTime == null);

        if (activeRecord == null)
        {
            throw new InvalidOperationException("No active punch-in found to punch out.");
        }

        activeRecord.PunchOutTime = DateTime.UtcNow;
        activeRecord.PunchOutLatitude = lat;
        activeRecord.PunchOutLongitude = lon;
        activeRecord.UpdatedDate = DateTime.UtcNow;

        // Calculate Overtime (If shift info available, otherwise basic calculation)
        var totalMinutes = (activeRecord.PunchOutTime.Value - activeRecord.PunchInTime).TotalMinutes;
        
        // Default logic: Example -> Anything above scheduled shift is overtime. 
        // For now tracking total time logged. Actual overtime logic can tie deeply into Shift Start/End times.
        activeRecord.OvertimeMinutes = totalMinutes; 

        _context.AttendanceRecords.Update(activeRecord);
        await _context.SaveChangesAsync();
        return activeRecord;
    }

    public async Task ProcessAutoPunchOutsAsync()
    {
        var autoOutHours = double.Parse(await _settingsService.GetSettingValueAsync("AutoPunchOutHours", "8"));
        var cutoffTime = DateTime.UtcNow.AddHours(-autoOutHours);

        var expiredRecords = await _context.AttendanceRecords
            .Where(a => a.PunchOutTime == null && a.PunchInTime <= cutoffTime)
            .ToListAsync();

        foreach (var record in expiredRecords)
        {
            // Auto punch out exact 8 hours after punch in
            record.PunchOutTime = record.PunchInTime.AddHours(autoOutHours);
            record.IsAutoPunchedOut = true;
            record.OvertimeMinutes = 0; // Standard 8 hours, no explicit overtime if auto-punched
            record.UpdatedDate = DateTime.UtcNow;
        }

        if (expiredRecords.Any())
        {
            _context.AttendanceRecords.UpdateRange(expiredRecords);
            await _context.SaveChangesAsync();
        }
    }
    
    public async Task<object?> GetActivePunchAsync(string vdlId)
    {
        return await _context.AttendanceRecords.FirstOrDefaultAsync(a => a.VdlId == vdlId && a.PunchOutTime == null);
    }
}