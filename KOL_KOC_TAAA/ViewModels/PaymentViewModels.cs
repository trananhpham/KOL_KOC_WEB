using System.ComponentModel.DataAnnotations;

namespace KOL_KOC_TAAA.ViewModels;

public class CheckoutViewModel
{
    public Guid BookingId { get; set; }
    public string BookingTitle { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";

    [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
    public string PaymentMethod { get; set; } = "E-Wallet";
}

public class PaymentResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;
    public Guid? BookingId { get; set; }
}
