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
        var seatRow = await _context.Set<SeatRow>().FindAsync(seatRowId);
        if (seatRow == null || seatRow.IsDeleted)
        {
            throw new ArgumentException("Seat row not found.");
        }

        if (seatRow.IsLocked)
        {
            throw new InvalidOperationException("Cannot add seat to a locked row.");
        }

        var nextOrder = await _context.Set<Seat>()
            .Where(s => s.SeatRowId == seatRowId && !s.IsDeleted)
            .Select(s => (int?)s.SeatOrder)
            .MaxAsync() ?? 0;

        var finalOrder = nextOrder + 1;
        var finalLabel = string.IsNullOrWhiteSpace(seatLabel)
            ? finalOrder.ToString()
            : seatLabel.Trim();

        var existingSeat = await _context.Set<Seat>()
            .FirstOrDefaultAsync(s => s.SeatRowId == seatRowId && s.SeatLabel == finalLabel);

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

        var seat = new Seat
        {
            SeatRowId = seatRowId,
            SeatLabel = finalLabel,
            SeatOrder = finalOrder,
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

    public async Task<SeatAssignment> CreateSeatAssignmentAsync(int seatId, int shiftId, int studentId, string createdBy)
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

        var student = await _context.Set<Student>().FindAsync(studentId);
        if (student == null)
        {
            throw new ArgumentException("Student not found.");
        }

        var existingStudentShiftAssignment = await _context.Set<SeatAssignment>()
            .AnyAsync(a => a.StudentId == studentId && a.ShiftId == shiftId && !a.IsDeleted);
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
            existingSeatAssignment.StudentId = studentId;
            existingSeatAssignment.AssignedDate = DateTime.UtcNow;
            existingSeatAssignment.RemovedDate = null;
            await _context.SaveChangesAsync();
            return existingSeatAssignment;
        }

        var assignment = new SeatAssignment
        {
            SeatId = seatId,
            ShiftId = shiftId,
            StudentId = studentId,
            CreatedBy = createdBy,
            IsDeleted = false,
            AssignedDate = DateTime.UtcNow
        };

        _context.Set<SeatAssignment>().Add(assignment);
        await _context.SaveChangesAsync();
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
            .Include(r => r.Seats.Where(s => includeDeleted || !s.IsDeleted))
                .ThenInclude(s => s.SeatAssignments.Where(a => includeDeleted || !a.IsDeleted))
                    .ThenInclude(a => a.Student)
            .Include(r => r.Seats.Where(s => includeDeleted || !s.IsDeleted))
                .ThenInclude(s => s.SeatAssignments.Where(a => includeDeleted || !a.IsDeleted))
                    .ThenInclude(a => a.Shift)
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
            Seats = r.Seats.OrderBy(s => s.SeatOrder).Select(s => new SeatResponse
            {
                Id = s.Id,
                SeatRowId = s.SeatRowId,
                SeatLabel = s.SeatLabel,
                SeatOrder = s.SeatOrder,
                IsLocked = s.IsLocked,
                IsDeleted = s.IsDeleted,
                Assignments = s.SeatAssignments.Select(a => new SeatAssignmentResponse
                {
                    Id = a.Id,
                    SeatId = a.SeatId,
                    ShiftId = a.ShiftId,
                    StudentId = a.StudentId,
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
