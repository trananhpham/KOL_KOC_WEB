using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using KOL_KOC_TAAA.Models;

namespace KOL_KOC_TAAA.ViewModels;

public class StudioDashboardViewModel
{
    public string FullName { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string InfluencerType { get; set; } = null!;
    public int ProfileCompleteness { get; set; } // 0-100%
    
    // Quick Stats
    public long TotalFollowers { get; set; }
    public decimal RatingAvg { get; set; }
    public int PendingRequestsCount { get; set; }
    public int UpcomingMeetingsCount { get; set; }
    public decimal WalletBalance { get; set; }
    
    // Growth Chart Data (Simplified)
    public List<int> MonthlyFollowersGain { get; set; } = new();
    
    // Recent Activities
    public List<StudioActivityViewModel> RecentActivities { get; set; } = new();
}

public class StudioActivityViewModel
{
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string Type { get; set; } = "info"; // info, success, warning, danger
}

public class PortfolioItemViewModel
{
    public Guid? Id { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập tiêu đề tác phẩm")]
    [Display(Name = "Tiêu đề")]
    public string Title { get; set; } = null!;
    
    [Display(Name = "Mô tả chi tiết")]
    public string? Description { get; set; }
    
    [Display(Name = "Đường dẫn Media (Ảnh/Video)")]
    public string? MediaUrl { get; set; }
    
    [Display(Name = "Loại hình")]
    public string? ContentType { get; set; } // Image, Video, Link
}
