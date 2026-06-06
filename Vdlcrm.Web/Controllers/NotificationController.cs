using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;
using Vdlcrm.Model;
using Vdlcrm.Model.DTOs;
using Vdlcrm.Services;
using Vdlcrm.Web.Hubs;
using System.Security.Claims;

namespace Vdlcrm.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationController(AppDbContext dbContext, IHubContext<NotificationHub> hubContext)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    // 1. Create Draft Notification (Admin/Internal Only)
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] NotificationCreateRequest request)
    {
        var createdBy = User.FindFirst(ClaimTypes.Name)?.Value ?? "System";

        var notification = new Notification
        {
            Title = request.Title,
            Message = request.Message,
            Type = request.Type,
            TargetAudience = request.TargetAudience,
            ExpiryDate = request.ExpiryDate,
            Status = "Draft",
            CreatedBy = createdBy,
            CreatedDate = DateTime.UtcNow
        };

        _dbContext.Set<Notification>().Add(notification);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Notification saved as Draft.", notification });
    }

    // 2. Edit Draft Notification (Admin/Internal Only)
    [HttpPut("update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] NotificationUpdateRequest request)
    {
        var notification = await _dbContext.Set<Notification>().FindAsync(id);
        if (notification == null) return NotFound(new { message = "Notification not found." });

        notification.Title = request.Title;
        notification.Message = request.Message;
        notification.Type = request.Type;
        notification.TargetAudience = request.TargetAudience;
        notification.ExpiryDate = request.ExpiryDate;
        notification.Status = request.Status;

        await _dbContext.SaveChangesAsync();
        return Ok(new { message = "Notification updated successfully.", notification });
    }

    // 3. Admin View: Get ALL Notifications (Live, Draft, Archived)
    [HttpGet("all")]
    public async Task<IActionResult> GetAll()
    {
        var notifications = await _dbContext.Set<Notification>()
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();
            
        return Ok(notifications);
    }

    // 4. Student View: Get only LIVE Notifications that are not expired
    [HttpGet("live")]
    public async Task<IActionResult> GetLiveNotifications()
    {
        var now = DateTime.UtcNow;
        var liveNotifications = await _dbContext.Set<Notification>()
            .Where(n => n.Status == "Live" && (n.ExpiryDate == null || n.ExpiryDate > now))
            .OrderByDescending(n => n.PublishDate)
            .ToListAsync();

        return Ok(liveNotifications);
    }

    // 5. Delete/Archive Notification
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var notification = await _dbContext.Set<Notification>().FindAsync(id);
        if (notification == null) return NotFound();

        _dbContext.Set<Notification>().Remove(notification);
        await _dbContext.SaveChangesAsync();

        return Ok(new { message = "Notification deleted." });
    }

    // 6. 🚀 PUBLISH ENDPOINT: Status 'Live' karega aur SignalR se alert bhejega
    [HttpPost("{id}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        var notification = await _dbContext.Set<Notification>().FindAsync(id);
        if (notification == null) return NotFound(new { message = "Notification not found." });

        if (notification.Status == "Live")
            return BadRequest(new { message = "Notification is already live." });

        notification.Status = "Live";
        notification.PublishDate = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        // SignalR ke through saare connected apps/browsers ko real-time event bhejna
        // Frontend mein hume "ReceiveNotification" event ko listen karna hoga
        await _hubContext.Clients.All.SendAsync("ReceiveNotification", new 
        {
            id = notification.Id,
            title = notification.Title,
            message = notification.Message,
            type = notification.Type,
            publishDate = notification.PublishDate
        });

        return Ok(new { message = "Notification is now LIVE and users have been alerted.", notification });
    }
}