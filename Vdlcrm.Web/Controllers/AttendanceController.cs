using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System;
using Vdlcrm.Services;

namespace Vdlcrm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly AttendanceService _attendanceService;

    public AttendanceController(AttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    [HttpPost("punch-in")]
    public async Task<IActionResult> PunchIn([FromBody] PunchRequest request)
    {
        try
        {
            var record = await _attendanceService.PunchInAsync(request.VdlId, request.Latitude, request.Longitude, request.ShiftId);
            return Ok(new { message = "Punched In Successfully", data = record });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("punch-out")]
    public async Task<IActionResult> PunchOut([FromBody] PunchRequest request)
    {
        try
        {
            var record = await _attendanceService.PunchOutAsync(request.VdlId, request.Latitude, request.Longitude);
            return Ok(new { message = "Punched Out Successfully", data = record });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("active/{vdlId}")]
    public async Task<IActionResult> GetActivePunch(string vdlId)
    {
        var record = await _attendanceService.GetActivePunchAsync(vdlId);
        return Ok(new { data = record });
    }
}

public class PunchRequest
{
    public string VdlId { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? ShiftId { get; set; }
}