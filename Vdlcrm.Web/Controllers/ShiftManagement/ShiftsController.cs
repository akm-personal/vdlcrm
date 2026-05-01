using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers.ShiftManagement;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ShiftsController : ControllerBase
{
    private readonly ShiftService _shiftService;
    private readonly ILogger<ShiftsController> _logger;

    public ShiftsController(ShiftService shiftService, ILogger<ShiftsController> logger)
    {
        _shiftService = shiftService;
        _logger = logger;
    }

    /// <summary>
    /// Get the current user's ID from JWT claims
    /// </summary>
    private int GetCurrentUserId()
    {
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var userIdClaim = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier);
        
        if (int.TryParse(userIdClaim?.Value, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User ID not found in token.");
    }

    /// <summary>
    /// Get the current user's role ID from JWT claims
    /// </summary>
    private int GetCurrentUserRoleId()
    {
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var roleIdClaim = claimsIdentity?.FindFirst("RoleId");
        
        if (int.TryParse(roleIdClaim?.Value, out var roleId))
        {
            return roleId;
        }

        throw new UnauthorizedAccessException("Role ID not found in token.");
    }

    /// <summary>
    /// Verify if user has allowed role (1=Admin, 2=Internal User)
    /// </summary>
    private bool IsUserAuthorized()
    {
        try
        {
            var roleId = GetCurrentUserRoleId();
            return roleId == 1 || roleId == 2;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a new shift (Admin and Internal User only)
    /// </summary>
    /// <param name="request">Shift creation request</param>
    /// <returns>Created shift</returns>
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateShiftResponse>> CreateShift([FromBody] CreateShiftRequest request)
    {
        if (!IsUserAuthorized())
        {
            _logger.LogWarning($"Unauthorized shift creation attempt by user with valid token");
            return Forbid("Only Admin and Internal User roles can create shifts.");
        }

        if (request == null || string.IsNullOrWhiteSpace(request.ShiftName))
        {
            return BadRequest(new { message = "Shift name is required." });
        }

        try
        {
            var userId = GetCurrentUserId();
            
            var shift = new Shift
            {
                ShiftName = request.ShiftName,
                Status = request.Status ?? 1, // 0=not active, 1=active, 2=deleted
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            var createdShift = await _shiftService.CreateShiftAsync(shift, userId);

            _logger.LogInformation($"Shift created by user {userId}: {createdShift.ShiftName}");

            return CreatedAtAction(nameof(GetShiftById), new { id = createdShift.Id }, new CreateShiftResponse
            {
                Id = createdShift.Id,
                ShiftName = createdShift.ShiftName,
                Status = createdShift.Status,
                StartTime = createdShift.StartTime,
                EndTime = createdShift.EndTime,
                CreatedDate = createdShift.CreatedDate,
                Message = "Shift created successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError($"Argument error creating shift: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating shift: {ex.Message}");
            return StatusCode(500, new { message = "Error creating shift", error = ex.Message });
        }
    }

    /// <summary>
    /// Update an existing shift (Admin and Internal User only)
    /// </summary>
    /// <param name="id">Shift ID to update</param>
    /// <param name="request">Shift update request</param>
    /// <returns>Updated shift</returns>
    [HttpPut("update/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UpdateShiftResponse>> UpdateShift(int id, [FromBody] UpdateShiftRequest request)
    {
        if (!IsUserAuthorized())
        {
            _logger.LogWarning($"Unauthorized shift update attempt by user with valid token");
            return Forbid("Only Admin and Internal User roles can update shifts.");
        }

        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid shift ID." });
        }

        if (request == null)
        {
            return BadRequest(new { message = "Update request cannot be null." });
        }

        try
        {
            var userId = GetCurrentUserId();

            var shift = new Shift
            {
                ShiftName = request.ShiftName ?? string.Empty,
                Status = request.Status ?? 0, // 0 means not provided, will keep existing value
                StartTime = request.StartTime,
                EndTime = request.EndTime
            };

            var updatedShift = await _shiftService.UpdateShiftAsync(id, shift, userId);

            if (updatedShift == null)
            {
                return NotFound(new { message = "Shift not found." });
            }

            _logger.LogInformation($"Shift updated by user {userId}: {updatedShift.ShiftName}");

            return Ok(new UpdateShiftResponse
            {
                Id = updatedShift.Id,
                ShiftName = updatedShift.ShiftName,
                Status = updatedShift.Status,
                StartTime = updatedShift.StartTime,
                EndTime = updatedShift.EndTime,
                UpdatedDate = updatedShift.UpdatedDate,
                Message = "Shift updated successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError($"Argument error updating shift: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"Invalid operation updating shift: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error updating shift: {ex.Message}");
            return StatusCode(500, new { message = "Error updating shift", error = ex.Message });
        }
    }

    /// <summary>
    /// Soft delete a shift (Admin and Internal User only)
    /// </summary>
    /// <param name="id">Shift ID to delete</param>
    /// <returns>Delete response</returns>
    [HttpDelete("delete/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteShiftResponse>> DeleteShift(int id)
    {
        if (!IsUserAuthorized())
        {
            _logger.LogWarning($"Unauthorized shift deletion attempt by user with valid token");
            return Forbid("Only Admin and Internal User roles can delete shifts.");
        }

        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid shift ID." });
        }

        try
        {
            var userId = GetCurrentUserId();
            
            var deleted = await _shiftService.SoftDeleteShiftAsync(id, userId);

            if (!deleted)
            {
                return NotFound(new { message = "Shift not found." });
            }

            _logger.LogInformation($"Shift soft deleted by user {userId}: ID={id}");

            return Ok(new DeleteShiftResponse
            {
                Id = id,
                Message = "Shift deleted successfully",
                IsDeleted = true,
                DeletedDate = DateTime.UtcNow
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError($"Argument error deleting shift: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"Invalid operation deleting shift: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting shift: {ex.Message}");
            return StatusCode(500, new { message = "Error deleting shift", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all shifts including inactive and deleted (Admin and Internal User only)
    /// </summary>
    /// <returns>List of all shifts</returns>
    [HttpGet("all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetShiftsResponse>> GetAllShifts()
    {
        if (!IsUserAuthorized())
        {
            _logger.LogWarning($"Unauthorized shifts fetch attempt by user with valid token");
            return Forbid("Only Admin and Internal User roles can view shifts.");
        }

        try
        {
            var shifts = await _shiftService.GetAllShiftsAsync(includeDeleted: true);

            return Ok(new GetShiftsResponse
            {
                Shifts = shifts.ToList(),
                Count = shifts.Count(),
                Message = "Shifts retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching shifts: {ex.Message}");
            return StatusCode(500, new { message = "Error fetching shifts", error = ex.Message });
        }
    }

    /// <summary>
    /// Get a shift by ID (Admin and Internal User only)
    /// </summary>
    /// <param name="id">Shift ID</param>
    /// <returns>Shift details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GetShiftByIdResponse>> GetShiftById(int id)
    {
        if (!IsUserAuthorized())
        {
            _logger.LogWarning($"Unauthorized shift fetch attempt by user with valid token");
            return Forbid("Only Admin and Internal User roles can view shift details.");
        }

        if (id <= 0)
        {
            return BadRequest(new { message = "Invalid shift ID." });
        }

        try
        {
            var shift = await _shiftService.GetShiftByIdAsync(id);

            if (shift == null)
            {
                return NotFound(new { message = "Shift not found." });
            }

            return Ok(new GetShiftByIdResponse
            {
                Id = shift.Id,
                ShiftName = shift.ShiftName,
                Status = shift.Status,
                StartTime = shift.StartTime,
                EndTime = shift.EndTime,
                CreatedBy = shift.CreatedBy,
                CreatedDate = shift.CreatedDate,
                UpdatedBy = shift.UpdatedBy,
                UpdatedDate = shift.UpdatedDate,
                Message = "Shift retrieved successfully"
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogError($"Argument error fetching shift: {ex.Message}");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching shift: {ex.Message}");
            return StatusCode(500, new { message = "Error fetching shift", error = ex.Message });
        }
    }
}
