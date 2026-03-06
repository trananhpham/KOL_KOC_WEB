using System.ComponentModel.DataAnnotations;

namespace KOL_KOC_TAAA.ViewModels;

public class SubmitDeliverableViewModel
{
    public Guid BookingId { get; set; }
    
    [Required]
    [Display(Name = "Loại sản phẩm")]
    public string DeliverableType { get; set; } = "Video";

    [Required]
    [Display(Name = "Tiêu đề sản phẩm")]
    public string Title { get; set; } = null!;

    [Display(Name = "Đường dẫn sản phẩm (Youtube, Facebook link...)")]
    [Url]
    public string? WorkUrl { get; set; }

    [Display(Name = "Mô tả/Ghi chú thêm")]
    public string? Description { get; set; }
}

public class DeliverableListViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public string? WorkUrl { get; set; }
}
