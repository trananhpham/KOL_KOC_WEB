using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class DeliverableController : Controller
{
    private readonly KolMarketplaceContext _context;

    public DeliverableController(KolMarketplaceContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [Authorize(Roles = "KOL")]
    public IActionResult Submit(Guid bookingId)
    {
        return View(new SubmitDeliverableViewModel { BookingId = bookingId });
    }

    [HttpPost]
    [Authorize(Roles = "KOL")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmitDeliverableViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();
        var booking = await _context.Bookings.FindAsync(model.BookingId);
        if (booking == null || booking.KolUserId != userId) return Unauthorized();

        var deliverable = new Deliverable
        {
            Id = Guid.NewGuid(),
            BookingId = model.BookingId,
            DeliverableType = model.DeliverableType,
            Title = model.Title,
            Description = model.Description + (string.IsNullOrEmpty(model.WorkUrl) ? "" : "\nURL: " + model.WorkUrl),
            Status = "Submitted",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Deliverables.Add(deliverable);

        // Notify Customer
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = booking.CustomerUserId,
            Type = "DeliverableSubmitted",
            Title = "KOL đã nộp sản phẩm bàn giao",
            Body = $"KOL đã nộp sản phẩm cho đơn hàng #{booking.Id.ToString().Substring(0, 8)}. Vui lòng kiểm tra và duyệt.",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Nộp sản phẩm thành công! Đang chờ khách hàng duyệt.";
        return RedirectToAction("Details", "Booking", new { id = model.BookingId });
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var userId = GetCurrentUserId();
        var deliverable = await _context.Deliverables.Include(d => d.Booking).FirstOrDefaultAsync(d => d.Id == id);
        
        if (deliverable == null || deliverable.Booking.CustomerUserId != userId) return Unauthorized();

        deliverable.Status = "Approved";
        deliverable.ApprovedAt = DateTime.UtcNow;
        deliverable.UpdatedAt = DateTime.UtcNow;

        // Save the current approval first
        await _context.SaveChangesAsync();

        // Now check if ALL deliverables for this booking are approved (including the one just saved)
        var allApproved = await _context.Deliverables.Where(d => d.BookingId == deliverable.BookingId).AllAsync(d => d.Status == "Approved");
        if (allApproved)
        {
            deliverable.Booking.Status = "completed";
            deliverable.Booking.UpdatedAt = DateTime.UtcNow;

            // Release funds to KOL wallet
            var kolWallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == deliverable.Booking.KolUserId);
            if (kolWallet == null)
            {
                kolWallet = new UserWallet { UserId = deliverable.Booking.KolUserId, Balance = 0, LockedBalance = 0, Currency = "VND", UpdatedAt = DateTime.UtcNow };
                _context.UserWallets.Add(kolWallet);
            }

            kolWallet.Balance += deliverable.Booking.AgreedSubtotal;
            kolWallet.UpdatedAt = DateTime.UtcNow;

            _context.WalletLedgers.Add(new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletUserId = kolWallet.UserId,
                Amount = deliverable.Booking.AgreedSubtotal,
                TransactionType = "BookingIncome",
                ReferenceId = deliverable.BookingId,
                Description = $"Thu nhập từ đơn hàng #{deliverable.BookingId.ToString().Substring(0, 8)}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Details", "Booking", new { id = deliverable.BookingId });
    }
}
