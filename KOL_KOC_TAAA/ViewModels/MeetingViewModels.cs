using System;
using System.ComponentModel.DataAnnotations;

namespace KOL_KOC_TAAA.ViewModels;

public class CreateMeetingViewModel
{
    [Required]
    public Guid BookingId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tiêu đề cuộc họp")]
    [Display(Name = "Tiêu đề cuộc họp")]
    public string Title { get; set; } = null!;

    [Display(Name = "Nội dung cuộc họp")]
    public string? Agenda { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian bắt đầu")]
    [Display(Name = "Thời gian bắt đầu")]
    public DateTime StartTime { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn thời gian kết thúc")]
    [Display(Name = "Thời gian kết thúc")]
    public DateTime EndTime { get; set; }

    [Display(Name = "Hình thức")]
    public string? MeetingType { get; set; } = "online";

    [Display(Name = "Link phòng họp (Zoom/Meet)")]
    public string? MeetingUrl { get; set; }
}
操控
操控
操控
