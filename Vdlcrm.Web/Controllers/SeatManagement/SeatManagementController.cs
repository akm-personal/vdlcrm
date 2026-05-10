using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Vdlcrm.Services;
using Vdlcrm.Model.DTOs;
using System.Security.Claims;
using System.Threading.Tasks;
using System;

namespace Vdlcrm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SeatManagementController : ControllerBase
{
    private readonly SeatManagementService _seatService;

    public SeatManagementController(SeatManagementService seatService)
    {
        _seatService = seatService;
    }

    private string GetCurrentVdlId()
    {
        // Token se logged-in user ka VDL ID nikalna
        return User.FindFirst(ClaimTypes.Name)?.Value ?? "system";
    }

    [HttpGet("layout")]
    public async Task<IActionResult> GetSeatLayout([FromQuery] bool includeDeleted = false)
    {
        var layout = await _seatService.GetSeatLayoutAsync(includeDeleted);
        return Ok(layout);
    }

    [HttpPost("rows/create")]
    public async Task<IActionResult> CreateRow([FromBody] CreateSeatRowRequest request)
    {
        var vdlId = GetCurrentVdlId();
        var row = await _seatService.CreateSeatRowAsync(request.RowName, vdlId, vdlId);
        return Ok(new SeatRowResponse 
        {
            Id = row.Id,
            RowName = row.RowName,
            RowOrder = row.RowOrder,
            IsLocked = row.IsLocked,
            IsDeleted = row.IsDeleted
        });
    }

    [HttpPut("rows/update/{id}")]
    public async Task<IActionResult> UpdateRow(int id, [FromBody] UpdateSeatRowRequest request)
    {
        var vdlId = GetCurrentVdlId();
        var row = await _seatService.UpdateSeatRowAsync(id, request, vdlId);
        if (row == null) return NotFound(new { message = "Row not found or deleted." });
        return Ok(new SeatRowResponse 
        {
            Id = row.Id,
            RowName = row.RowName,
            RowOrder = row.RowOrder,
            IsLocked = row.IsLocked,
            IsDeleted = row.IsDeleted
        });
    }

    [HttpDelete("rows/delete/{id}")]
    public async Task<IActionResult> DeleteRow(int id)
    {
        var vdlId = GetCurrentVdlId();
        var success = await _seatService.DeleteSeatRowAsync(id, vdlId);
        if (!success) return NotFound(new { message = "Row not found or already deleted." });
        return Ok(new { message = "Row deleted successfully." });
    }

    [HttpPost("seats/create")]
    public async Task<IActionResult> CreateSeat([FromBody] CreateSeatRequest request)
    {
        try
        {
            var vdlId = GetCurrentVdlId();
            var seat = await _seatService.CreateSeatAsync(request.SeatRowId, request.SeatLabel, vdlId, vdlId);
            return Ok(new SeatResponse
            {
                Id = seat.Id,
                SeatRowId = seat.SeatRowId,
                SeatLabel = seat.SeatLabel,
                SeatOrder = seat.SeatOrder,
                IsLocked = seat.IsLocked,
                IsDeleted = seat.IsDeleted
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("seats/update/{id}")]
    public async Task<IActionResult> UpdateSeat(int id, [FromBody] UpdateSeatRequest request)
    {
        var vdlId = GetCurrentVdlId();
        var seat = await _seatService.UpdateSeatAsync(id, request, vdlId);
        if (seat == null) return NotFound(new { message = "Seat not found or deleted." });
        return Ok(new SeatResponse
        {
            Id = seat.Id,
            SeatRowId = seat.SeatRowId,
            SeatLabel = seat.SeatLabel,
            SeatOrder = seat.SeatOrder,
            IsLocked = seat.IsLocked,
            IsDeleted = seat.IsDeleted
        });
    }

    [HttpDelete("seats/delete/{id}")]
    public async Task<IActionResult> DeleteSeat(int id)
    {
        var vdlId = GetCurrentVdlId();
        var success = await _seatService.DeleteSeatAsync(id, vdlId);
        if (!success) return NotFound(new { message = "Seat not found or already deleted." });
        return Ok(new { message = "Seat deleted successfully." });
    }

    [HttpPost("assignments/create")]
    public async Task<IActionResult> AssignSeat([FromBody] CreateSeatAssignmentRequest request)
    {
        try
        {
            var vdlId = GetCurrentVdlId();
            var assignment = await _seatService.CreateSeatAssignmentAsync(request.SeatId, request.ShiftId, request.StudentId, vdlId);
            return Ok(new SeatAssignmentResponse
            {
                Id = assignment.Id,
                SeatId = assignment.SeatId,
                ShiftId = assignment.ShiftId,
                StudentId = assignment.StudentId,
                IsDeleted = assignment.IsDeleted,
                AssignedDate = assignment.AssignedDate
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("assignments/delete/{id}")]
    public async Task<IActionResult> RemoveAssignment(int id)
    {
        var success = await _seatService.DeleteSeatAssignmentAsync(id);
        if (!success) return NotFound(new { message = "Assignment not found or already deleted." });
        return Ok(new { message = "Assignment removed successfully." });
    }
}