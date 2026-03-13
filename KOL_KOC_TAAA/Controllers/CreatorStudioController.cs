using System.Security.Claims;
using KOL_KOC_TAAA.Services;
using KOL_KOC_TAAA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KOL_KOC_TAAA.Controllers;

[Authorize(Roles = "KOL")]
public class CreatorStudioController : Controller
{
    private readonly IKolProfileService _profileService;

    public CreatorStudioController(IKolProfileService profileService)
    {
        _profileService = profileService;
    }

    public async Task<IActionResult> Index()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var model = await _profileService.GetStudioDashboardAsync(userId);
            return View(model);
        }
        return Challenge();
    }

    public async Task<IActionResult> EditProfile()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var profile = await _profileService.GetProfileByUserIdAsync(userId);
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
        return Challenge();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(KolProfileEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var success = await _profileService.UpdateProfileAsync(userId, model);
            if (success)
            {
                TempData["SuccessMessage"] = "Hồ sơ đã được cập nhật thành công!";
                return RedirectToAction(nameof(Index));
            }
        }
        return View(model);
    }

    public async Task<IActionResult> Portfolio()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var portfolios = await _profileService.GetPortfoliosAsync(userId);
            return View(portfolios);
        }
        return Challenge();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPortfolio(PortfolioItemViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(Portfolio));

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            await _profileService.AddPortfolioAsync(userId, model);
            TempData["SuccessMessage"] = "Đã thêm tác phẩm vào Portfolio!";
        }
        return RedirectToAction(nameof(Portfolio));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePortfolio(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            await _profileService.DeletePortfolioAsync(userId, id);
            TempData["SuccessMessage"] = "Đã xóa tác phẩm khỏi Portfolio.";
        }
        return RedirectToAction(nameof(Portfolio));
    }

    public async Task<IActionResult> SocialAccounts()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var accounts = await _profileService.GetSocialAccountsAsync(userId);
            return View(accounts);
        }
        return Challenge();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSocialAccount(KolSocialAccountViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(SocialAccounts));

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            await _profileService.AddSocialAccountAsync(userId, model);
            TempData["SuccessMessage"] = "Đã liên kết tài khoản mạng xã hội thành công!";
        }
        return RedirectToAction(nameof(SocialAccounts));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveSocialAccount(Guid id)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            await _profileService.RemoveSocialAccountAsync(userId, id);
            TempData["SuccessMessage"] = "Đã hủy liên kết tài khoản.";
        }
        return RedirectToAction(nameof(SocialAccounts));
    }

    public async Task<IActionResult> RateCard()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            var rateCards = await _profileService.GetRateCardsAsync(userId);
            return View(rateCards);
        }
        return Challenge();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRateCard(RateCardViewModel model)
    {
        if (!ModelState.IsValid) return RedirectToAction(nameof(RateCard));

        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdStr, out var userId))
        {
            await _profileService.CreateRateCardAsync(userId, model);
            TempData["SuccessMessage"] = "Đã tạo gói dịch vụ mới!";
        }
        return RedirectToAction(nameof(RateCard));
    }
}
