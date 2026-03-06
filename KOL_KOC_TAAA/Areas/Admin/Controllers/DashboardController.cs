using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;

namespace KOL_KOC_TAAA.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly KolMarketplaceContext _context;

    public DashboardController(KolMarketplaceContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.TotalKols = await _context.KolProfiles.CountAsync();
        ViewBag.TotalBookings = await _context.Bookings.CountAsync();
        ViewBag.PlatformRevenue = await _context.Bookings.SumAsync(b => b.PlatformFee);
        
        var recentBookings = await _context.Bookings
            .Include(b => b.CustomerUser)
            .Include(b => b.KolUser.User)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToListAsync();

        return View(recentBookings);
    }
}
