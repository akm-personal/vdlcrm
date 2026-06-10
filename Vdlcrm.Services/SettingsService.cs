using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class SettingsService
{
    private readonly AppDbContext _context;

    public SettingsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<string> GetSettingValueAsync(string key, string defaultValue = "")
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        return setting?.Value ?? defaultValue;
    }

    public async Task UpdateSettingAsync(string key, string value)
    {
        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            _context.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedAt = System.DateTime.UtcNow });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = System.DateTime.UtcNow;
            _context.AppSettings.Update(setting);
        }
        await _context.SaveChangesAsync();
    }
}