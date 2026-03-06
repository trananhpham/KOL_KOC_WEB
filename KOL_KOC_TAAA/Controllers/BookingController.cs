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

        // Add the selected item to BookingRequestItems
        var requestItem = new BookingRequestItem
        {
            Id = Guid.NewGuid(),
            BookingRequestId = booking.Id,
            ServiceType = serviceItem.ServiceType,
            Platform = serviceItem.Platform,
            Quantity = 1,
            ExpectedUnitPrice = serviceItem.UnitPrice
        };
        _context.BookingRequestItems.Add(requestItem);

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
            .Include(r => r.Booking)
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
            .Include(r => r.Booking)
            .Where(r => r.KolUserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return View(requests);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = GetCurrentUserId();
        var booking = await _context.Bookings
            .Include(b => b.KolUser.User)
            .Include(b => b.CustomerUser)
            .Include(b => b.BookingItems)
            .Include(b => b.Contracts)
            .Include(b => b.Deliverables)
            .Include(b => b.BookingRequest)
            .FirstOrDefaultAsync(b => b.Id == id && (b.CustomerUserId == userId || b.KolUserId == userId));

        if (booking == null) return NotFound();

        return View(booking);
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

            if (status == "accepted")
            {
                // Create official Booking
                var requestItems = await _context.BookingRequestItems
                    .Where(i => i.BookingRequestId == requestId)
                    .ToListAsync();

                decimal subtotal = requestItems.Sum(i => (i.ExpectedUnitPrice ?? 0) * i.Quantity);
                decimal platformFee = subtotal * 0.1m; // 10% fee
                decimal total = subtotal + platformFee;

                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    BookingRequestId = requestId,
                    CustomerUserId = request.CustomerUserId,
                    KolUserId = request.KolUserId,
                    AgreedSubtotal = subtotal,
                    PlatformFee = platformFee,
                    TaxAmount = 0, // Placeholder
                    TotalAmount = total,
                    Currency = request.Currency,
                    Status = "pending_contract",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Bookings.Add(booking);

                // Add BookingItems
                foreach (var ri in requestItems)
                {
                    _context.BookingItems.Add(new BookingItem
                    {
                        Id = Guid.NewGuid(),
                        BookingId = booking.Id,
                        ServiceType = ri.ServiceType,
                        Platform = ri.Platform,
                        Quantity = ri.Quantity,
                        UnitPrice = ri.ExpectedUnitPrice ?? 0,
                        LineTotal = (ri.ExpectedUnitPrice ?? 0) * ri.Quantity
                    });
                }
            }

            // 3. Notify Customer
            _context.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = request.CustomerUserId,
                Type = status == "accepted" ? "BookingAccepted" : "BookingDeclined",
                Title = status == "accepted" ? "Yêu cầu đã được chấp nhận" : "Yêu cầu đã bị từ chối",
                Body = status == "accepted"
                    ? $"KOL đã chấp nhận yêu cầu của bạn. Vui lòng thanh toán để bắt đầu."
                    : $"KOL đã từ chối yêu cầu của bạn.",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = status == "accepted" ? "Đã chấp nhận yêu cầu và khởi tạo đơn hàng." : "Đã từ chối yêu cầu.";
        }

        return RedirectToAction(nameof(ManageBookings));
    }
}
