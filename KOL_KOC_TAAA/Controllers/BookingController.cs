using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;
using KOL_KOC_TAAA.Services;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IFinanceService _financeService;

    public BookingController(IBookingService bookingService, IFinanceService financeService)
    {
        _bookingService = bookingService;
        _financeService = financeService;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var model = new BookingListViewModel();
        
        if (User.IsInRole("Customer"))
        {
            model.SentRequests = await _bookingService.GetCustomerBookingRequestsAsync(userId);
        }
        
        if (User.IsInRole("KOL"))
        {
            model.ReceivedRequests = await _bookingService.GetKolBookingRequestsAsync(userId);
        }

        return View(model);
    }

    public async Task<IActionResult> Detail(Guid id)
    {
        var booking = await _bookingService.GetBookingAsync(id);
        if (booking == null) return NotFound();

        var userId = GetCurrentUserId();
        if (booking.CustomerUserId != userId && booking.KolUserId != userId) return Forbid();

        var model = new BookingDetailViewModel
        {
            Booking = booking,
            Role = booking.KolUserId == userId ? "KOL" : "Customer",
            IsContractSigned = booking.Contracts.Any(c => c.Status == "signed"),
            IsPaymentReceived = booking.Status != "pending_payment" && booking.Status != "awaiting_payment",
            HasDeliverablesSubmitted = booking.Deliverables.Any(),
            IsCompleted = booking.Status == "completed"
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "KOL")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(Guid requestId, string status)
    {
        var userId = GetCurrentUserId();
        var request = await _bookingService.GetBookingRequestAsync(requestId);
        
        if (request == null || request.KolUserId != userId) return NotFound();

        if (status == "accepted")
        {
            var booking = await _bookingService.FinalizeBookingFromRequestAsync(requestId);
            TempData["SuccessMessage"] = "Bạn đã chấp nhận yêu cầu và tạo đơn hàng chính thức.";
            return RedirectToAction(nameof(Detail), new { id = booking.Id });
        }
        else if (status == "rejected")
        {
            await _bookingService.UpdateBookingRequestStatusAsync(requestId, "rejected");
            TempData["SuccessMessage"] = "Đã từ chối yêu cầu hợp tác.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Pay(Guid id)
    {
        var booking = await _bookingService.GetBookingAsync(id);
        if (booking == null || booking.CustomerUserId != GetCurrentUserId()) return NotFound();

        return View(booking);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPayment(Guid bookingId)
    {
        var userId = GetCurrentUserId();
        var booking = await _bookingService.GetBookingAsync(bookingId);
        
        if (booking == null || booking.CustomerUserId != userId) return NotFound();

        var success = await _financeService.ProcessPaymentAsync(bookingId, userId, booking.TotalAmount);

        if (success)
        {
            TempData["SuccessMessage"] = "Thanh toán thành công. Tiền đã được giữ tại Escrow hệ thống.";
            return RedirectToAction(nameof(Detail), new { id = bookingId });
        }

        ModelState.AddModelError("", "Thanh toán thất bại. Vui lòng thử lại.");
        return View("Pay", booking);
    }
}
