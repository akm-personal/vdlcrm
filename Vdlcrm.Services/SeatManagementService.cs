using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;

namespace Vdlcrm.Services;

public class SeatManagementService
{
    private readonly AppDbContext _context;

    public SeatManagementService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SeatRow> CreateSeatRowAsync(string? rowName, string createdBy, string vdlId)
    {
        var nextOrder = await _context.Set<SeatRow>()
            .Where(r => !r.IsDeleted)
            .Select(r => (int?)r.RowOrder)
            .MaxAsync() ?? 0;

        var rowNumber = nextOrder + 1;
        var finalRowName = string.IsNullOrWhiteSpace(rowName)
            ? $"Row {rowNumber}"
            : rowName.Trim();

        var seatRow = new SeatRow
        {
            RowName = finalRowName,
            RowOrder = rowNumber,
            IsLocked = false,
            IsDeleted = false,
            CreatedBy = createdBy,
            UpdatedBy = vdlId,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Set<SeatRow>().Add(seatRow);
        await _context.SaveChangesAsync();
        return seatRow;
    }

    public async Task<SeatRow?> UpdateSeatRowAsync(int id, UpdateSeatRowRequest request, string vdlId)
    {
        var seatRow = await _context.Set<SeatRow>().FindAsync(id);
        if (seatRow == null || seatRow.IsDeleted)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.RowName))
        {
            seatRow.RowName = request.RowName.Trim();
        }

        if (request.IsLocked.HasValue)
        {
            seatRow.IsLocked = request.IsLocked.Value;
        }

        seatRow.UpdatedBy = vdlId;
        seatRow.UpdatedDate = DateTime.UtcNow;

        _context.Set<SeatRow>().Update(seatRow);
        await _context.SaveChangesAsync();
        return seatRow;
    }

    public async Task<bool> DeleteSeatRowAsync(int id, string vdlId)
    {
        var seatRow = await _context.Set<SeatRow>()
            .Include(r => r.Seats)
            .ThenInclude(s => s.SeatAssignments)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (seatRow == null || seatRow.IsDeleted)
        {
            return false;
        }

        seatRow.IsDeleted = true;
        seatRow.UpdatedBy = vdlId;
        seatRow.UpdatedDate = DateTime.UtcNow;

        foreach (var seat in seatRow.Seats)
        {
            seat.IsDeleted = true;
            seat.UpdatedBy = vdlId;
            seat.UpdatedDate = DateTime.UtcNow;

            foreach (var assignment in seat.SeatAssignments)
            {
                assignment.IsDeleted = true;
                assignment.RemovedDate = DateTime.UtcNow;
            }
        }

        _context.Set<SeatRow>().Update(seatRow);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Seat> CreateSeatAsync(int seatRowId, string? seatLabel, string createdBy, string vdlId)
    {
        // 1. Find the requested row
        var seatRow = await _context.Set<SeatRow>().FindAsync(seatRowId);

        if (seatRow == null)
        {
            throw new InvalidOperationException($"Row does not exist.");
        }
        else if (seatRow.IsDeleted)
        {
            seatRow.IsDeleted = false;
            seatRow.UpdatedBy = vdlId;
            seatRow.UpdatedDate = DateTime.UtcNow;
            _context.Set<SeatRow>().Update(seatRow);
            await _context.SaveChangesAsync();
        }

        if (seatRow.IsLocked)
        {
            throw new InvalidOperationException($"Row {seatRow.RowOrder} is locked. Cannot auto-add seat.");
        }

        // 2. Count seats in THIS row to find next order
        var seatsInRow = await _context.Set<Seat>().Where(s => s.SeatRowId == seatRow.Id).CountAsync();
        if (seatsInRow >= 20)
        {
            throw new InvalidOperationException($"Row {seatRow.RowOrder} already has 20 seats. You cannot add more.");
        }

        int seatOrderInRow = seatsInRow + 1;
        int globalSeatNumber = ((seatRow.RowOrder - 1) * 20) + seatOrderInRow;

        // 3. Generate final label based on RowOrder and SeatOrderInRow (e.g., R2-1), unless explicitly provided
        string finalLabel = string.IsNullOrWhiteSpace(seatLabel) ? $"R{seatRow.RowOrder}-{seatOrderInRow}" : seatLabel.Trim();

        // 4. Check if for some reason this exact seat already exists (Safeguard)
        var existingSeat = await _context.Set<Seat>()
            .FirstOrDefaultAsync(s => s.SeatRowId == seatRow.Id && s.SeatLabel == finalLabel);

        if (existingSeat != null)
        {
            if (existingSeat.IsDeleted)
            {
                existingSeat.IsDeleted = false;
                existingSeat.UpdatedBy = vdlId;
                existingSeat.UpdatedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return existingSeat;
            }
            throw new InvalidOperationException($"A seat with label '{finalLabel}' already exists in this row.");
        }

        // 5. Final seat object create karein
        var seat = new Seat
        {
            SeatRowId = seatRow.Id,
            SeatLabel = finalLabel,
            SeatOrder = globalSeatNumber,
            IsLocked = false,
            IsDeleted = false,
            CreatedBy = createdBy,
            UpdatedBy = vdlId,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Set<Seat>().Add(seat);
        await _context.SaveChangesAsync();
        
        return seat;
    }

    public async Task<List<SeatResponse>> CreateSeatsBulkAsync(int seatRowId, int count, string createdBy, string updatedBy)
    {
        // 1. Find the requested row
        var seatRow = await _context.Set<SeatRow>().FindAsync(seatRowId);

        if (seatRow == null)
        {
            throw new InvalidOperationException($"Row does not exist.");
        }
        else if (seatRow.IsDeleted)
        {
            seatRow.IsDeleted = false;
            seatRow.UpdatedBy = updatedBy;
            seatRow.UpdatedDate = DateTime.UtcNow;
            _context.Set<SeatRow>().Update(seatRow);
            await _context.SaveChangesAsync();
        }

        if (seatRow.IsLocked)
        {
            throw new InvalidOperationException($"Row {seatRow.RowOrder} is locked. Cannot auto-add seats.");
        }

        // 2. Current row me kitni seats already hain
        int seatsAlreadyInThisRow = await _context.Set<Seat>().Where(s => s.SeatRowId == seatRow.Id).CountAsync();
        int availableSpots = 20 - seatsAlreadyInThisRow;

        if (availableSpots <= 0)
        {
            throw new InvalidOperationException($"Row {seatRow.RowOrder} already has 20 seats. You cannot add more.");
        }

        // 3. Cap the count
        int actualCountToCreate = Math.Min(count, availableSpots);

        var newSeats = new List<Seat>();
        var responseList = new List<SeatResponse>();

        for (int i = 0; i < actualCountToCreate; i++)
        {
            int seatOrderInRow = seatsAlreadyInThisRow + i + 1;
            int globalSeatNumber = ((seatRow.RowOrder - 1) * 20) + seatOrderInRow;
            string finalLabel = $"R{seatRow.RowOrder}-{seatOrderInRow}";

            var seat = new Seat
            {
                SeatRowId = seatRow.Id,
                SeatLabel = finalLabel,
                SeatOrder = globalSeatNumber,
                IsLocked = false,
                IsDeleted = false,
                CreatedBy = createdBy,
                UpdatedBy = updatedBy,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            newSeats.Add(seat);
        }

        await _context.Seats.AddRangeAsync(newSeats);
        await _context.SaveChangesAsync();

        foreach (var seat in newSeats)
        {
            responseList.Add(new SeatResponse
            {
                Id = seat.Id,
                SeatRowId = seat.SeatRowId,
                SeatLabel = seat.SeatLabel,
                SeatOrder = seat.SeatOrder,
                IsLocked = seat.IsLocked,
                IsDeleted = seat.IsDeleted
            });
        }

        return responseList;
    }

    public async Task<Seat?> UpdateSeatAsync(int id, UpdateSeatRequest request, string vdlId)
    {
        var seat = await _context.Set<Seat>().FindAsync(id);
        if (seat == null || seat.IsDeleted)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.SeatLabel))
        {
            seat.SeatLabel = request.SeatLabel.Trim();
        }

        if (request.IsLocked.HasValue)
        {
            seat.IsLocked = request.IsLocked.Value;
        }

        seat.UpdatedBy = vdlId;
        seat.UpdatedDate = DateTime.UtcNow;

        _context.Set<Seat>().Update(seat);
        await _context.SaveChangesAsync();
        return seat;
    }

    public async Task<bool> DeleteSeatAsync(int id, string vdlId)
    {
        var seat = await _context.Set<Seat>()
            .Include(s => s.SeatAssignments)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (seat == null || seat.IsDeleted)
        {
            return false;
        }

        seat.IsDeleted = true;
        seat.UpdatedBy = vdlId;
        seat.UpdatedDate = DateTime.UtcNow;

        foreach (var assignment in seat.SeatAssignments)
        {
            assignment.IsDeleted = true;
            assignment.RemovedDate = DateTime.UtcNow;
        }

        _context.Set<Seat>().Update(seat);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<SeatAssignment> CreateSeatAssignmentAsync(int seatId, int shiftId, string studentVdlId, string createdBy)
    {
        var seat = await _context.Set<Seat>().Include(s => s.SeatRow).FirstOrDefaultAsync(s => s.Id == seatId);
        if (seat == null || seat.IsDeleted)
        {
            throw new ArgumentException("Seat not found.");
        }

        if (seat.IsLocked)
        {
            throw new InvalidOperationException("This seat is locked and cannot be assigned.");
        }

        if (seat.SeatRow?.IsLocked == true)
        {
            throw new InvalidOperationException("The row containing this seat is locked.");
        }

        var shift = await _context.Set<Shift>().FindAsync(shiftId);
        if (shift == null || shift.IsDeleted == true)
        {
            throw new ArgumentException("Shift not found.");
        }

        // Fix: FindAsync expects primary key (Id = int). Use FirstOrDefaultAsync for VdlId (string).
        var student = await _context.Set<Student>().FirstOrDefaultAsync(s => s.VdlId == studentVdlId);
        if (student == null)
        {
            throw new ArgumentException("Student not found.");
        }

        var existingStudentShiftAssignment = await _context.Set<SeatAssignment>()
            .AnyAsync(a => a.StudentVdlId == studentVdlId && a.ShiftId == shiftId && !a.IsDeleted);
        if (existingStudentShiftAssignment)
        {
            throw new InvalidOperationException("This student already has a seat assigned for the selected shift.");
        }

        var existingSeatAssignment = await _context.Set<SeatAssignment>()
            .FirstOrDefaultAsync(a => a.SeatId == seatId && a.ShiftId == shiftId);

        if (existingSeatAssignment != null)
        {
            if (!existingSeatAssignment.IsDeleted)
            {
                throw new InvalidOperationException("This seat is already assigned for the selected shift.");
            }
            existingSeatAssignment.IsDeleted = false;
            existingSeatAssignment.StudentVdlId = student.VdlId;
            existingSeatAssignment.AssignedDate = DateTime.UtcNow;
            existingSeatAssignment.RemovedDate = null;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                throw new InvalidOperationException($"Database error: {ex.InnerException?.Message ?? ex.Message}");
            }
            return existingSeatAssignment;
        }

        var assignment = new SeatAssignment
        {
            SeatId = seatId,
            ShiftId = shiftId,
            StudentVdlId = studentVdlId, 
            CreatedBy = createdBy,
            IsDeleted = false,
            AssignedDate = DateTime.UtcNow
        };

        _context.Set<SeatAssignment>().Add(assignment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            throw new InvalidOperationException($"Database error: {ex.InnerException?.Message ?? ex.Message}");
        }
        return assignment;
    }

    public async Task<bool> DeleteSeatAssignmentAsync(int id)
    {
        var assignment = await _context.Set<SeatAssignment>().FindAsync(id);
        if (assignment == null || assignment.IsDeleted)
        {
            return false;
        }

        assignment.IsDeleted = true;
        assignment.RemovedDate = DateTime.UtcNow;
        _context.Set<SeatAssignment>().Update(assignment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<SeatRowResponse>> GetSeatLayoutAsync(bool includeDeleted = false)
    {
        var query = _context.Set<SeatRow>()
            .Include(r => r.Seats)
                .ThenInclude(s => s.SeatAssignments)
                    .ThenInclude(a => a.Student)
            .Include(r => r.Seats)
                .ThenInclude(s => s.SeatAssignments)
                    .ThenInclude(a => a.Shift)
            .AsSplitQuery()
            .AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(r => !r.IsDeleted);
        }

        var rows = await query
            .OrderBy(r => r.RowOrder)
            .ThenBy(r => r.Id)
            .ToListAsync();

        return rows.Select(r => new SeatRowResponse
        {
            Id = r.Id,
            RowName = r.RowName,
            RowOrder = r.RowOrder,
            IsLocked = r.IsLocked,
            IsDeleted = r.IsDeleted,
            Seats = (r.Seats ?? Enumerable.Empty<Seat>())
                .Where(s => includeDeleted || !s.IsDeleted)
                .OrderBy(s => s.SeatOrder)
                .Select(s => new SeatResponse
            {
                Id = s.Id,
                SeatRowId = s.SeatRowId,
                SeatLabel = s.SeatLabel,
                SeatOrder = s.SeatOrder,
                IsLocked = s.IsLocked,
                IsDeleted = s.IsDeleted,
                Assignments = (s.SeatAssignments ?? Enumerable.Empty<SeatAssignment>())
                    .Where(a => includeDeleted || !a.IsDeleted)
                    .Select(a => new SeatAssignmentResponse
                {
                    Id = a.Id,
                    SeatId = a.SeatId,
                    ShiftId = a.ShiftId,                    
                    StudentName = a.Student?.Name,
                    StudentVdlId = a.Student?.VdlId,
                    ShiftName = a.Shift?.ShiftName,
                    IsDeleted = a.IsDeleted,
                    AssignedDate = a.AssignedDate
                }).ToList()
            }).ToList()
        }).ToList();
    }

    public async Task<Seat?> GetSeatByIdAsync(int id)
    {
        return await _context.Set<Seat>().FindAsync(id);
    }

    public async Task<SeatRow?> GetSeatRowByIdAsync(int id)
    {
        return await _context.Set<SeatRow>().FindAsync(id);
    }

    public async Task<SeatAssignment?> GetSeatAssignmentByIdAsync(int id)
    {
        return await _context.Set<SeatAssignment>().FindAsync(id);
    }
}
