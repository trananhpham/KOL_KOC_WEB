using System.ComponentModel.DataAnnotations;

namespace KOL_KOC_TAAA.ViewModels;

public class CreateBookingViewModel
{
    public Guid KolUserId { get; set; }
    public string KolName { get; set; } = null!;
    
    [Required]
    [Display(Name = "Dịch vụ chọn")]
    public Guid RateCardItemId { get; set; }
    
    public List<BookingRateCardItemViewModel> AvailableServices { get; set; } = new();

    [Required(ErrorMessage = "Vui lòng nhập mô tả chiến dịch")]
    [Display(Name = "Mô tả chiến dịch/Yêu cầu công việc")]
    public string CampaignDescription { get; set; } = null!;

    [Required]
    [Display(Name = "Ngày dự kiến bắt đầu")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(7));

    [Required]
    [Display(Name = "Ngày dự kiến kết thúc")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Now.AddDays(14));

    [Display(Name = "Ghi chú thêm")]
    public string? Notes { get; set; }
}

public class BookingRateCardItemViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
}

public class BookingListViewModel
{
    public Guid BookingId { get; set; }
    public string KolName { get; set; } = null!;
    public string BrandName { get; set; } = null!;
    public string ServiceName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
