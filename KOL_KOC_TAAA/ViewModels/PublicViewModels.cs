using KOL_KOC_TAAA.Models;

namespace KOL_KOC_TAAA.ViewModels;

public class PublicKolProfileViewModel
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string InfluencerType { get; set; } = null!;
    public string? Bio { get; set; }
    public string? LocationCity { get; set; }
    public string? LocationCountry { get; set; }
    public decimal RatingAvg { get; set; }
    public int RatingCount { get; set; }
    public bool IsVerified { get; set; }
    public decimal? MinBudget { get; set; }

    public List<KolSocialAccount> SocialAccounts { get; set; } = new List<KolSocialAccount>();
    public List<KolPortfolio> Portfolios { get; set; } = new List<KolPortfolio>();
    public List<RateCard> RateCards { get; set; } = new List<RateCard>();
}
