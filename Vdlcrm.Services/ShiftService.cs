using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vdlcrm.Model;

namespace Vdlcrm.Services;

public class ShiftService
{
    private readonly AppDbContext _context;

    public ShiftService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Shift> CreateShiftAsync(Shift shift, string vdlId)
    {
        shift.CreatedBy = vdlId;
        shift.CreatedDate = DateTime.UtcNow;
        shift.UpdatedBy = vdlId;
        shift.UpdatedDate = DateTime.UtcNow;
        shift.IsDeleted = false;
        
        _context.Set<Shift>().Add(shift);
        await _context.SaveChangesAsync();
        return shift;
    }

    public async Task<Shift?> UpdateShiftAsync(int id, Shift updatedShift, string vdlId)
    {
        var existingShift = await _context.Set<Shift>().FindAsync(id);
        if (existingShift == null) return null;

        existingShift.ShiftName = updatedShift.ShiftName;
        existingShift.Status = updatedShift.Status;
        existingShift.StartTime = updatedShift.StartTime;
        existingShift.EndTime = updatedShift.EndTime;
        existingShift.UpdatedBy = vdlId;
        existingShift.UpdatedDate = DateTime.UtcNow;

        _context.Set<Shift>().Update(existingShift);
        await _context.SaveChangesAsync();
        return existingShift;
    }

    public async Task<bool> SoftDeleteShiftAsync(int id, string vdlId)
    {
        var shift = await _context.Set<Shift>().FindAsync(id);
        if (shift == null) return false;

        shift.IsDeleted = true;
        shift.Status = 2; // 2 = Deleted Status
        shift.UpdatedBy = vdlId;
        shift.UpdatedDate = DateTime.UtcNow;

        _context.Set<Shift>().Update(shift);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<Shift>> GetAllShiftsAsync(bool includeDeleted = false)
    {
        var query = _context.Set<Shift>().AsQueryable();
        
        if (!includeDeleted)
        {
            query = query.Where(s => s.IsDeleted != true);
        }
        
        return await query.ToListAsync(); 
    }

    public async Task<Shift?> GetShiftByIdAsync(int id)
    {
        return await _context.Set<Shift>().FindAsync(id);
    }
}