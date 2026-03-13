using System.Security.Claims;
using KOL_KOC_TAAA.Services;
using KOL_KOC_TAAA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class MeetingController : Controller
{
    private readonly IMeetingService _meetingService;
    private readonly IBookingService _bookingService;

    public MeetingController(IMeetingService meetingService, IBookingService bookingService)
    {
        _meetingService = meetingService;
        _bookingService = bookingService;
    }

    public async Task<IActionResult> Schedule(Guid bookingId)
    {
        var booking = await _bookingService.GetBookingAsync(bookingId);
        if (booking == null) return NotFound();

        var model = new CreateMeetingViewModel
        {
            BookingId = bookingId,
            Title = $"Họp thảo luận: {booking.BookingRequest.Title}",
            StartTime = DateTime.Now.AddDays(1).Date.AddHours(9),
            EndTime = DateTime.Now.AddDays(1).Date.AddHours(10)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Schedule(CreateMeetingViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId)) return Challenge();

        try
        {
            var meeting = await _meetingService.CreateMeetingAsync(userId, model);
            TempData["SuccessMessage"] = "Đã lên lịch họp thành công!";
            return RedirectToAction("Detail", "Booking", new { id = model.BookingId });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Lỗi: " + ex.Message);
            return View(model);
        }
    }
}
操控
操控
操控
