using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize(Roles = "KOL")]
public class KolProfileController : Controller
{
    private readonly KolMarketplaceContext _context;

    public KolProfileController(KolMarketplaceContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var profile = await _context.KolProfiles
            .Include(p => p.KolSocialAccounts)
            .Include(p => p.RateCards)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null) return NotFound("Profile not found");

        return View(profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var userId = GetCurrentUserId();
        var profile = await _context.KolProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        var model = new KolProfileEditViewModel
        {
            InfluencerType = profile.InfluencerType,
            Bio = profile.Bio,
            Gender = profile.Gender,
            Dob = profile.Dob,
            LocationCity = profile.LocationCity,
            LocationCountry = profile.LocationCountry,
            MinBudget = profile.MinBudget
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(KolProfileEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();
        var profile = await _context.KolProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return NotFound();

        profile.InfluencerType = model.InfluencerType;
        profile.Bio = model.Bio;
        profile.Gender = model.Gender;
        profile.Dob = model.Dob;
        profile.LocationCity = model.LocationCity;
        profile.LocationCountry = model.LocationCountry;
        profile.MinBudget = model.MinBudget;
        profile.UpdatedAt = DateTime.UtcNow;

        _context.KolProfiles.Update(profile);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Socials()
    {
        var userId = GetCurrentUserId();
        var accounts = await _context.KolSocialAccounts
            .Where(s => s.KolUserId == userId)
            .Select(s => new KolSocialAccountViewModel
            {
                Id = s.Id,
                Platform = s.Platform,
                Url = s.ProfileUrl ?? "",
                FollowersCount = (int?)s.Followers
            })
            .ToListAsync();

        return View(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> AddSocial(KolSocialAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông tin không hợp lệ.";
            return RedirectToAction(nameof(Socials));
        }

        var userId = GetCurrentUserId();
        var social = new KolSocialAccount
        {
            Id = Guid.NewGuid(),
            KolUserId = userId,
            Platform = model.Platform,
            ProfileUrl = model.Url,
            Followers = model.FollowersCount
        };

        _context.KolSocialAccounts.Add(social);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã thêm mạng xã hội.";
        return RedirectToAction(nameof(Socials));
    }

    [HttpPost]
    public async Task<IActionResult> RemoveSocial(Guid id)
    {
        var userId = GetCurrentUserId();
        var social = await _context.KolSocialAccounts.FirstOrDefaultAsync(s => s.Id == id && s.KolUserId == userId);
        if (social != null)
        {
            _context.KolSocialAccounts.Remove(social);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa mạng xã hội.";
        }
        return RedirectToAction(nameof(Socials));
    }

    [HttpGet]
    public async Task<IActionResult> RateCards()
    {
        var userId = GetCurrentUserId();
        var rateCards = await _context.RateCards
            .Include(r => r.RateCardItems)
            .Where(r => r.KolUserId == userId)
            .ToListAsync();

        return View(rateCards);
    }

    [HttpPost]
    public async Task<IActionResult> CreateRateCard(RateCardViewModel model)
    {
        if (ModelState.IsValid)
        {
            var userId = GetCurrentUserId();
            var rc = new RateCard
            {
                Id = Guid.NewGuid(),
                KolUserId = userId,
                Title = model.Title,
                Currency = model.Currency,
                IsActive = model.IsActive
            };
            _context.RateCards.Add(rc);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Tạo Rate Card thành công.";
        }
        return RedirectToAction(nameof(RateCards));
    }
}
