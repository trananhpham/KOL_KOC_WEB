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
    private readonly KolMarketplaceContext _context;

    public BookingController(KolMarketplaceContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> Create(Guid kolId)
    {
        var kol = await _context.KolProfiles
            .Include(p => p.User)
            .Include(p => p.RateCards)
                .ThenInclude(r => r.RateCardItems)
            .FirstOrDefaultAsync(p => p.UserId == kolId);

        if (kol == null) return NotFound();

        var model = new CreateBookingViewModel
        {
            KolUserId = kol.UserId,
            KolName = kol.User.FullName ?? kol.User.Email,
            AvailableServices = kol.RateCards.SelectMany(r => r.RateCardItems).Select(i => new BookingRateCardItemViewModel
            {
                Id = i.Id,
                Name = $"{i.ServiceType} ({i.Platform})",
                Price = i.UnitPrice
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "Customer")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateBookingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            // Re-populate services if model is invalid
            var kol = await _context.KolProfiles
                .Include(p => p.RateCards).ThenInclude(r => r.RateCardItems)
                .FirstOrDefaultAsync(p => p.UserId == model.KolUserId);
            if (kol != null)
            {
                model.AvailableServices = kol.RateCards.SelectMany(r => r.RateCardItems).Select(i => new BookingRateCardItemViewModel
                {
                    Id = i.Id, Name = $"{i.ServiceType} ({i.Platform})", Price = i.UnitPrice
                }).ToList();
            }
            return View(model);
        }

        var customerId = GetCurrentUserId();
        var serviceItem = await _context.RateCardItems.FindAsync(model.RateCardItemId);
        if (serviceItem == null) return NotFound("Dịch vụ không tồn tại");

        var booking = new BookingRequest
        {
            Id = Guid.NewGuid(),
            CustomerUserId = customerId,
            KolUserId = model.KolUserId,
            Status = "pending",
            Title = $"Booking từ {User.FindFirstValue(ClaimTypes.Name)}",
            Brief = model.CampaignDescription + (string.IsNullOrEmpty(model.Notes) ? "" : "\nNotes: " + model.Notes),
            ProposedStartDate = model.StartDate,
            ProposedEndDate = model.EndDate,
            Currency = "VND",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Note: The schema has BookingRequests and Bookings separately. 
        // For simple flow, we'll create a BookingRequest first.
        _context.BookingRequests.Add(booking);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Gửi yêu cầu booking thành công! Vui lòng chờ KOL phản hồi.";
        return RedirectToAction(nameof(MyRequests));
    }

    [HttpGet]
    [Authorize(Roles = "Customer")]
    public async Task<IActionResult> MyRequests()
    {
        var userId = GetCurrentUserId();
        var requests = await _context.BookingRequests
            .Include(r => r.KolUser).ThenInclude(u => u.User)
            .Where(r => r.CustomerUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpGet]
    [Authorize(Roles = "KOL")]
    public async Task<IActionResult> ManageBookings()
    {
        var userId = GetCurrentUserId();
        var requests = await _context.BookingRequests
            .Include(r => r.CustomerUser)
            .Where(r => r.KolUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpPost]
    [Authorize(Roles = "KOL")]
    public async Task<IActionResult> Respond(Guid requestId, string status)
    {
        var userId = GetCurrentUserId();
        var request = await _context.BookingRequests.FirstOrDefaultAsync(r => r.Id == requestId && r.KolUserId == userId);
        
        if (request == null) return NotFound();

        if (status == "accepted" || status == "declined")
        {
            request.Status = status;
            request.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = status == "accepted" ? "Đã chấp nhận yêu cầu." : "Đã từ chối yêu cầu.";
        }

        return RedirectToAction(nameof(ManageBookings));
    }
}
