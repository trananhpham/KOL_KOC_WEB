using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;

namespace KOL_KOC_TAAA.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class KolManagementController : Controller
{
    private readonly KolMarketplaceContext _context;

    public KolManagementController(KolMarketplaceContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var kols = await _context.KolProfiles
            .Include(k => k.User)
            .OrderByDescending(k => k.CreatedAt)
            .ToListAsync();
        return View(kols);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVerify(Guid userId)
    {
        var kol = await _context.KolProfiles.FindAsync(userId);
        if (kol != null)
        {
            kol.IsVerified = !kol.IsVerified;
            kol.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã {(kol.IsVerified ? "xác thực" : "hủy xác thực")} KOL thành công.";
        }
        return RedirectToAction(nameof(Index));
    }
}
