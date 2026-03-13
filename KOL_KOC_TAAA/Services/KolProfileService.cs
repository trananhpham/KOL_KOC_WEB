using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace KOL_KOC_TAAA.Services;

public class KolProfileService : IKolProfileService
{
    private readonly KolMarketplaceContext _context;

    public KolProfileService(KolMarketplaceContext context)
    {
        _context = context;
    }

    public async Task<KolProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _context.KolProfiles
            .Include(p => p.KolSocialAccounts)
            .Include(p => p.RateCards)
                .ThenInclude(rc => rc.RateCardItems)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    public async Task<bool> UpdateProfileAsync(Guid userId, KolProfileEditViewModel model)
    {
        var profile = await _context.KolProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return false;

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
        return true;
    }

    public async Task<List<KolSocialAccount>> GetSocialAccountsAsync(Guid userId)
    {
        return await _context.KolSocialAccounts
            .Where(s => s.KolUserId == userId)
            .ToListAsync();
    }

    public async Task<bool> AddSocialAccountAsync(Guid userId, KolSocialAccountViewModel model)
    {
        var social = new KolSocialAccount
        {
            Id = Guid.NewGuid(),
            KolUserId = userId,
            Platform = model.Platform,
            ProfileUrl = model.Url,
            Followers = model.FollowersCount,
            CreatedAt = DateTime.UtcNow
        };

        _context.KolSocialAccounts.Add(social);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveSocialAccountAsync(Guid userId, Guid socialId)
    {
        var social = await _context.KolSocialAccounts.FirstOrDefaultAsync(s => s.Id == socialId && s.KolUserId == userId);
        if (social == null) return false;

        _context.KolSocialAccounts.Remove(social);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<RateCard>> GetRateCardsAsync(Guid userId)
    {
        return await _context.RateCards
            .Include(r => r.RateCardItems)
            .Where(r => r.KolUserId == userId)
            .ToListAsync();
    }

    public async Task<bool> CreateRateCardAsync(Guid userId, RateCardViewModel model)
    {
        var rc = new RateCard
        {
            Id = Guid.NewGuid(),
            KolUserId = userId,
            Title = model.Title,
            Currency = model.Currency ?? "VND",
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.RateCards.Add(rc);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StudioDashboardViewModel> GetStudioDashboardAsync(Guid userId)
    {
        var profile = await _context.KolProfiles
            .Include(p => p.User)
            .Include(p => p.KolSocialAccounts)
            .Include(p => p.KolPortfolios)
            .Include(p => p.RateCards)
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null) throw new Exception("Profile not found");

        var model = new StudioDashboardViewModel
        {
            FullName = profile.User.FullName ?? profile.User.Email,
            AvatarUrl = profile.User.AvatarUrl,
            InfluencerType = profile.InfluencerType,
            RatingAvg = profile.RatingAvg,
            TotalFollowers = profile.KolSocialAccounts.Sum(s => s.Followers ?? 0),
            
            PendingRequestsCount = await _context.BookingRequests.CountAsync(r => r.KolUserId == userId && r.Status == "sent"),
            UpcomingMeetingsCount = await _context.MeetingParticipants.CountAsync(mp => mp.UserId == userId && mp.Meeting.Status == "scheduled"),
            WalletBalance = (await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId))?.Balance ?? 0
        };

        // Calculate Completeness
        int score = 0;
        if (!string.IsNullOrEmpty(profile.User.AvatarUrl)) score += 20;
        if (!string.IsNullOrEmpty(profile.Bio) && profile.Bio.Length > 20) score += 20;
        if (profile.KolSocialAccounts.Any()) score += 20;
        if (profile.KolPortfolios.Any()) score += 20;
        if (profile.RateCards.Any()) score += 20;
        
        model.ProfileCompleteness = score;

        model.RecentActivities = new List<StudioActivityViewModel>
        {
            new StudioActivityViewModel { Title = "Hồ sơ đã cập nhật", Description = "Bạn vừa cập nhật thông tin Bio hôm nay.", CreatedAt = DateTime.Now.AddHours(-2), Type = "info" },
            new StudioActivityViewModel { Title = "Yêu cầu mới", Description = "Có một brand vừa gửi yêu cầu hợp tác cho bạn.", CreatedAt = DateTime.Now.AddDays(-1), Type = "success" }
        };

        return model;
    }

    public async Task<List<KolPortfolio>> GetPortfoliosAsync(Guid userId)
    {
        return await _context.KolPortfolios
            .Where(p => p.KolUserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> AddPortfolioAsync(Guid userId, PortfolioItemViewModel model)
    {
        var portfolio = new KolPortfolio
        {
            Id = Guid.NewGuid(),
            KolUserId = userId,
            Title = model.Title,
            Description = model.Description,
            MediaUrl = model.MediaUrl,
            ContentType = model.ContentType ?? "image",
            CreatedAt = DateTime.UtcNow
        };

        _context.KolPortfolios.Add(portfolio);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeletePortfolioAsync(Guid userId, Guid portfolioId)
    {
        var portfolio = await _context.KolPortfolios.FirstOrDefaultAsync(p => p.Id == portfolioId && p.KolUserId == userId);
        if (portfolio == null) return false;

        _context.KolPortfolios.Remove(portfolio);
        await _context.SaveChangesAsync();
        return true;
    }
}
