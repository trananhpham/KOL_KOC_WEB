using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;

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

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyRequests()
    {
        var userId = GetCurrentUserId();
        var requests = await _bookingService.GetCustomerBookingRequestsAsync(userId);
        return View(requests);
    }

    [HttpGet]
    [Authorize(Roles = "KOL")]
    public async Task<IActionResult> ManageBookings()
    {
        var userId = GetCurrentUserId();
        var kolId = userId; // Assuming userId is kolProfile.UserId
        var requests = await _bookingService.GetKolBookingRequestsAsync(kolId);
        return View(requests);
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
            await _bookingService.FinalizeBookingFromRequestAsync(requestId);
            TempData["SuccessMessage"] = "Bạn đã chấp nhận yêu cầu và tạo đơn hàng chính thức.";
        }
        else if (status == "rejected")
        {
            await _bookingService.UpdateBookingRequestStatusAsync(requestId, "rejected");
            TempData["SuccessMessage"] = "Đã từ chối yêu cầu hợp tác.";
        }

        return RedirectToAction(nameof(ManageBookings));
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Pay(Guid bookingId)
    {
        var booking = await _bookingService.GetBookingAsync(bookingId);
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
            return RedirectToAction(nameof(MyRequests));
        }

        ModelState.AddModelError("", "Thanh toán thất bại. Vui lòng thử lại.");
        return View("Pay", booking);
    }
}
}
