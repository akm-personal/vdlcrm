using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;
using Microsoft.EntityFrameworkCore;

namespace Vdlcrm.Web.Controllers.FeeManagement;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeeController : ControllerBase
{
    private readonly FeeService _feeService;
    private readonly ILogger<FeeController> _logger;
    private readonly AppDbContext _dbContext;
    private readonly ErrorLoggingService _errorLoggingService;

    public FeeController(FeeService feeService, ILogger<FeeController> logger, AppDbContext dbContext, ErrorLoggingService errorLoggingService)
    {
        _feeService = feeService;
        _logger = logger;
        _dbContext = dbContext;
        _errorLoggingService = errorLoggingService;
    }

    /// <summary>
    /// Get the current user's VDL ID from JWT claims
    /// </summary>
    private string GetCurrentVdlId()
    {
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var usernameClaim = claimsIdentity?.FindFirst(ClaimTypes.Name);
        
        if (!string.IsNullOrWhiteSpace(usernameClaim?.Value))
        {
            return usernameClaim.Value;
        }

        throw new UnauthorizedAccessException("User identity not found in token.");
    }

    /// <summary>
    /// Create a new fee record for a student
    /// </summary>
    [HttpPost("record")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> CreateFeeRecord([FromBody] CreateFeeRecordRequest request)
    {
        try
        {
            var vdlId = GetCurrentVdlId();

            // Validate that the VDL ID belongs to a Student (Role 4)
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == request.VdlId.ToLower());

            if (user == null)
            {
                return NotFound(new { message = $"User with VDL ID '{request.VdlId}' not found." });
            }

            if (user.RoleId != 4)
            {
                return BadRequest(new { message = $"Cannot create fee record. User '{request.VdlId}' has Role ID {user.RoleId}, but must be a Student (Role 4)." });
            }

            var record = await _feeService.CreateFeeRecordAsync(request, vdlId);

            return CreatedAtAction(nameof(GetStudentFeeRecords), new { vdlId = request.VdlId }, new 
            {
                success = true,
                message = "Fee record created successfully.",
                recordId = record.Id,
                vdlId = record.VdlId,
                totalFee = record.TotalFee,
                status = record.Status.ToString(),
                createdDate = record.CreatedDate
            });
        }
        catch (ArgumentException ex)
        {
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating fee record: {ex.Message}");
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return StatusCode(500, new { message = "An error occurred while creating the fee record.", error = ex.Message });
        }
    }

    /// <summary>
    /// Add a payment to an existing fee record
    /// </summary>
    [HttpPost("payment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> AddFeePayment([FromBody] AddFeePaymentRequest request)
    {
        try
        {
            var vdlId = GetCurrentVdlId();
            var payment = await _feeService.AddFeePaymentAsync(
                request.FeeRecordId, 
                request.AmountPaid, 
                request.PaymentMode, 
                request.Note, 
                vdlId
            );

            return Ok(new 
            {
                success = true,
                message = "Fee payment added successfully.",
                paymentId = payment.Id,
                feeRecordId = payment.FeeRecordId,
                amountPaid = payment.AmountPaid,
                paymentDate = payment.PaymentDate
            });
        }
        catch (ArgumentException ex)
        {
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding fee payment: {ex.Message}");
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return StatusCode(500, new { message = "An error occurred while processing the fee payment.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get the total fee balance summary for a student
    /// </summary>
    [HttpGet("student/{vdlId}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetStudentFeeBalance(string vdlId)
    {
        // Row-Level Security: Ensure Role 4 (Student) can only view their own balance
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var roleIdClaim = claimsIdentity?.FindFirst("RoleId");
        var usernameClaim = claimsIdentity?.FindFirst(ClaimTypes.Name); 
        
        if (int.TryParse(roleIdClaim?.Value, out int roleId) && roleId == 4)
        {
            if (!string.Equals(vdlId, usernameClaim?.Value, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access Denied: You can only view your own fee balance." });
            }
        }

        try
        {
            var (TotalFee, TotalPaid, Balance) = await _feeService.GetStudentFeeBalanceAsync(vdlId);
            return Ok(new 
            {
                VdlId = vdlId,
                TotalFee = TotalFee,
                TotalPaid = TotalPaid,
                Balance = Balance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching student balance: {ex.Message}");
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return StatusCode(500, new { message = "An error occurred while fetching the balance.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all fee records and associated payments for a student
    /// </summary>
    [HttpGet("student/{vdlId}/records")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> GetStudentFeeRecords(string vdlId)
    {
        // Row-Level Security: Ensure Role 4 (Student) can only view their own records
        var claimsIdentity = User.Identity as ClaimsIdentity;
        var roleIdClaim = claimsIdentity?.FindFirst("RoleId");
        var usernameClaim = claimsIdentity?.FindFirst(ClaimTypes.Name); 
        
        if (int.TryParse(roleIdClaim?.Value, out int roleId) && roleId == 4)
        {
            if (!string.Equals(vdlId, usernameClaim?.Value, StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access Denied: You can only view your own fee records." });
            }
        }

        try
        {
            var records = await _feeService.GetFeeRecordsByStudentAsync(vdlId);
            
            var result = records.Select(r => new {
                id = r.Id,
                vdlId = r.VdlId,
                totalFee = r.TotalFee,
                startDate = r.StartDate,
                endDate = r.EndDate,
                status = r.Status.ToString(),
                description = r.Description,
                createdByName = r.CreatedByName,
                createdByVdlId = r.CreatedByVdlId,
                createdDate = r.CreatedDate,
                updatedDate = r.UpdatedDate,
                payments = r.FeePayments.Select(p => new {
                    id = p.Id,
                    amountPaid = p.AmountPaid,
                    paymentMode = p.PaymentMode,
                    paymentDate = p.PaymentDate,
                    description = p.Note,
                    collectedByName = p.CollectedByName,
                    collectedByVdlId = p.CollectedByVdlId
                })
            });
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching student fee records: {ex.Message}");
            await _errorLoggingService.LogExceptionAsync(ex, HttpContext.Request.Path);
            return StatusCode(500, new { message = "An error occurred while fetching the fee records.", error = ex.Message });
        }
    }
}   
