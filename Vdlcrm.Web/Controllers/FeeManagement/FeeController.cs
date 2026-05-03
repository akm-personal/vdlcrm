using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers.FeeManagement;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FeeController : ControllerBase
{
    private readonly FeeService _feeService;
    private readonly ILogger<FeeController> _logger;

    public FeeController(FeeService feeService, ILogger<FeeController> logger)
    {
        _feeService = feeService;
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
    /// Create a new fee record for a student
    /// </summary>
    [HttpPost("record")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FeeRecord>> CreateFeeRecord([FromBody] CreateFeeRecordRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var record = await _feeService.CreateFeeRecordAsync(
                request.StudentId, 
                request.TotalFee, 
                request.StartDate, 
                request.EndDate, 
                request.Description, 
                userId
            );

            return CreatedAtAction(nameof(GetStudentFeeRecords), new { studentId = request.StudentId }, record);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating fee record: {ex.Message}");
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
    public async Task<ActionResult<FeePayment>> AddFeePayment([FromBody] AddFeePaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var payment = await _feeService.AddFeePaymentAsync(
                request.FeeRecordId, 
                request.AmountPaid, 
                request.PaymentMode, 
                request.Note, 
                userId
            );

            return Ok(payment);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error adding fee payment: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while processing the fee payment.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get the total fee balance summary for a student
    /// </summary>
    [HttpGet("student/{studentId}/balance")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<FeeBalanceResponse>> GetStudentFeeBalance(int studentId)
    {
        try
        {
            var (TotalFee, TotalPaid, Balance) = await _feeService.GetStudentFeeBalanceAsync(studentId);
            return Ok(new FeeBalanceResponse
            {
                StudentId = studentId,
                TotalFee = TotalFee,
                TotalPaid = TotalPaid,
                Balance = Balance
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching student balance: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while fetching the balance.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get all fee records and associated payments for a student
    /// </summary>
    [HttpGet("student/{studentId}/records")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<FeeRecord>>> GetStudentFeeRecords(int studentId)
    {
        try
        {
            var records = await _feeService.GetFeeRecordsByStudentAsync(studentId);
            return Ok(records);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error fetching student fee records: {ex.Message}");
            return StatusCode(500, new { message = "An error occurred while fetching the fee records.", error = ex.Message });
        }
    }
}