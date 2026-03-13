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
}
