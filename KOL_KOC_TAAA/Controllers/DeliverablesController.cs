using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class DeliverablesController : Controller
{
    private readonly KolMarketplaceContext _context;
    private readonly INotificationService _notificationService;
    private readonly IFinanceService _financeService;

    public DeliverablesController(KolMarketplaceContext context, 
                                INotificationService notificationService,
                                IFinanceService financeService)
    {
        _context = context;
        _notificationService = notificationService;
        _financeService = financeService;
    }

    private Guid GetCurrentUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Manage(Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.Deliverables)
            .Include(b => b.KolUser.User)
            .Include(b => b.CustomerUser)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) return NotFound();
        
        var userId = GetCurrentUserId();
        if (booking.CustomerUserId != userId && booking.KolUserId != userId) return Forbid();

        return View(booking);
    }

    [HttpPost]
    [Authorize(Roles = "KOL")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid bookingId, string contentUrl, string notes)
    {
        var userId = GetCurrentUserId();
        var booking = await _context.Bookings.FindAsync(bookingId);
        
        if (booking == null || booking.KolUserId != userId) return NotFound();

        var deliverable = new Deliverable
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Title = contentUrl, // Using Title to store URL
            Description = notes, // Using Description for notes
            Status = "submitted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Deliverables.Add(deliverable);
        booking.Status = "work_submitted";
        
        await _context.SaveChangesAsync();

        // Notify Brand
        await _notificationService.SendNotificationAsync(booking.CustomerUserId,
            "Sản phẩm mới đã được nộp",
            $"KOL đã nộp sản phẩm cho đơn hàng #{bookingId.ToString()[..8].ToUpper()}. Vui lòng kiểm tra và nghiệm thu.",
            "work_submitted");

        return RedirectToAction(nameof(Manage), new { bookingId });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid bookingId)
    {
        var userId = GetCurrentUserId();
        var booking = await _context.Bookings.FindAsync(bookingId);
        
        if (booking == null || booking.CustomerUserId != userId) return NotFound();

        booking.Status = "completed";
        await _context.SaveChangesAsync();

        // Execute Escrow Release
        var success = await _financeService.ReleaseEscrowToKolAsync(bookingId);

        if (success)
        {
            // Notify KOL
            await _notificationService.SendNotificationAsync(booking.KolUserId,
                "Dự án đã hoàn thành & Giải ngân",
                $"Nhãn hàng đã nghiệm thu sản phẩm. Tiền đã được chuyển vào ví của bạn.",
                "payment_released");

            TempData["SuccessMessage"] = "Đã nghiệm thu sản phẩm và giải ngân cho KOL.";
        }
        else
        {
            TempData["ErrorMessage"] = "Có lỗi xảy ra khi giải ngân. Vui lòng liên hệ Admin.";
        }

        return RedirectToAction("MyRequests", "Booking");
    }
}
