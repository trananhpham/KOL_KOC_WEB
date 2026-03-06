using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly KolMarketplaceContext _context;

    public NotificationController(KolMarketplaceContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> GetLatest()
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == GetCurrentUserId())
            .OrderByDescending(n => n.CreatedAt)
            .Take(5)
            .ToListAsync();

        return Json(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification != null && notification.UserId == GetCurrentUserId())
        {
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        return Ok();
    }
}
