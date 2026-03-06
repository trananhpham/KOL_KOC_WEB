using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace KOL_KOC_TAAA.Controllers
{
    public class HomeController : Controller
    {
        private readonly KolMarketplaceContext _context;

        public HomeController(KolMarketplaceContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Get some featured KOLs (e.g., highly rated or just the newest ones)
            var featuredKols = await _context.KolProfiles
                .Include(p => p.User)
                .Where(p => p.User.Status == "active")
                .OrderByDescending(p => p.RatingAvg)
                .Take(6)
                .Select(p => new PublicKolProfileViewModel
                {
                    UserId = p.UserId,
                    FullName = p.User.FullName ?? p.User.Email,
                    InfluencerType = p.InfluencerType,
                    LocationCity = p.LocationCity,
                    RatingAvg = p.RatingAvg,
                    RatingCount = p.RatingCount,
                    IsVerified = p.IsVerified,
                    MinBudget = p.MinBudget
                })
                .ToListAsync();

            return View(featuredKols);
        }

        public async Task<IActionResult> Search(string? query, string? type, string? city)
        {
            var kolsQuery = _context.KolProfiles
                .Include(p => p.User)
                .Where(p => p.User.Status == "active")
                .AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                kolsQuery = kolsQuery.Where(p => 
                    (p.User.FullName != null && p.User.FullName.Contains(query)) ||
                    (p.Bio != null && p.Bio.Contains(query)));
            }

            if (!string.IsNullOrEmpty(type))
            {
                kolsQuery = kolsQuery.Where(p => p.InfluencerType == type);
            }

            if (!string.IsNullOrEmpty(city))
            {
                kolsQuery = kolsQuery.Where(p => p.LocationCity == city);
            }

            var results = await kolsQuery
                .Select(p => new PublicKolProfileViewModel
                {
                    UserId = p.UserId,
                    FullName = p.User.FullName ?? p.User.Email,
                    InfluencerType = p.InfluencerType,
                    LocationCity = p.LocationCity,
                    RatingAvg = p.RatingAvg,
                    RatingCount = p.RatingCount,
                    IsVerified = p.IsVerified,
                    MinBudget = p.MinBudget
                })
                .ToListAsync();

            ViewBag.Query = query;
            ViewBag.Type = type;
            ViewBag.City = city;

            return View(results);
        }

        public async Task<IActionResult> KolDetail(Guid id)
        {
            var p = await _context.KolProfiles
                .Include(p => p.User)
                .Include(p => p.KolSocialAccounts)
                .Include(p => p.KolPortfolios)
                .Include(p => p.RateCards.Where(r => r.IsActive))
                    .ThenInclude(r => r.RateCardItems)
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (p == null || p.User.Status != "active") return NotFound();

            var model = new PublicKolProfileViewModel
            {
                UserId = p.UserId,
                FullName = p.User.FullName ?? p.User.Email,
                InfluencerType = p.InfluencerType,
                Bio = p.Bio,
                LocationCity = p.LocationCity,
                LocationCountry = p.LocationCountry,
                RatingAvg = p.RatingAvg,
                RatingCount = p.RatingCount,
                IsVerified = p.IsVerified,
                MinBudget = p.MinBudget,
                SocialAccounts = p.KolSocialAccounts.ToList(),
                Portfolios = p.KolPortfolios.ToList(),
                RateCards = p.RateCards.ToList()
            };

            return View(model);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
