using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using KOL_KOC_TAAA.Services;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize(Roles = "Customer")]
public class PaymentController : Controller
{
    private readonly KolMarketplaceContext _context;
    private readonly IMomoService _momoService;

    public PaymentController(KolMarketplaceContext context, IMomoService momoService)
    {
        _context = context;
        _momoService = momoService;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> Checkout(Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.BookingRequest)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerUserId == GetCurrentUserId());

        if (booking == null) return NotFound();
        if (booking.Status == "paid" || booking.Status == "completed") 
            return BadRequest("Đơn hàng này đã được thanh toán hoặc đã hoàn thành.");

        var model = new CheckoutViewModel
        {
            BookingId = booking.Id,
            BookingTitle = booking.BookingRequest?.Title ?? "Booking Service",
            Amount = booking.TotalAmount,
            Currency = booking.Currency
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessCheckout(CheckoutViewModel model)
    {
        var userId = GetCurrentUserId();
        var booking = await _context.Bookings
            .Include(b => b.BookingRequest)
            .FirstOrDefaultAsync(b => b.Id == model.BookingId && b.CustomerUserId == userId);

        if (booking == null) return NotFound();

        if (model.PaymentMethod == "Momo")
        {
            var response = await _momoService.CreatePaymentAsync(booking.Id, booking.BookingRequest?.Title ?? "Booking", booking.TotalAmount);
            if (response.resultCode == 0)
            {
                return Redirect(response.payUrl);
            }
            else
            {
                ModelState.AddModelError("", "Lỗi kết nối MoMo: " + response.message);
                return View("Checkout", model);
            }
        }

        // Default Simulated Payment (Simulated Bank/E-Wallet)
        await CompletePaymentData(booking, model.PaymentMethod);
        await _context.SaveChangesAsync();

        return RedirectToAction("Success", new { bookingId = booking.Id });
    }

    [HttpGet]
    public async Task<IActionResult> MomoReturn(MomoExecuteResponseModel response)
    {
        if (!_momoService.ValidateSignature(response))
        {
            return BadRequest("Chữ ký MoMo không hợp lệ.");
        }

        // MoMo orderId is "bookingId_timestamp"
        var bookingIdStr = response.orderId.Split('_')[0];
        if (!Guid.TryParse(bookingIdStr, out var bookingId)) return BadRequest("Mã đơn hàng không hợp lệ.");

        var booking = await _context.Bookings.FindAsync(bookingId);
        if (booking == null) return NotFound();

        if (response.resultCode == 0)
        {
            await CompletePaymentData(booking, "Momo");
            await _context.SaveChangesAsync();
            return RedirectToAction("Success", new { bookingId = booking.Id });
        }
        else
        {
            return RedirectToAction("Checkout", new { bookingId = booking.Id });
        }
    }

    private async Task CompletePaymentData(Booking booking, string method)
    {
        // 1. Create Invoice
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            InvoiceNo = "INV-" + DateTime.UtcNow.Ticks.ToString().Substring(10),
            Subtotal = booking.AgreedSubtotal,
            Fee = booking.PlatformFee,
            Tax = booking.TaxAmount,
            Total = booking.TotalAmount,
            Currency = booking.Currency,
            Status = "paid",
            IssuedAt = DateTime.UtcNow,
            PaidAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Invoices.Add(invoice);

        // 2. Create Payment Intent & Payment
        var intent = new PaymentIntent
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoice.Id,
            Provider = method == "Momo" ? "Momo" : "Manual_Simulated",
            ProviderIntentId = "pi_" + Guid.NewGuid().ToString("N"),
            Amount = invoice.Total,
            Currency = invoice.Currency,
            MethodType = method,
            Status = "succeeded",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.PaymentIntents.Add(intent);

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            PaymentIntentId = intent.Id,
            ProviderChargeId = "ch_" + Guid.NewGuid().ToString("N"),
            PaidAmount = intent.Amount,
            PaidAt = DateTime.UtcNow,
            Status = "succeeded",
            CreatedAt = DateTime.UtcNow
        };
        _context.Payments.Add(payment);

        // 3. Update Booking Status
        booking.Status = "paid";
        booking.UpdatedAt = DateTime.UtcNow;

        // 4. Notify KOL
        _context.Notifications.Add(new Notification
        {
            Id = Guid.NewGuid(),
            UserId = booking.KolUserId,
            Type = "PaymentReceived",
            Title = "Bạn có đơn hàng mới đã thanh toán",
            Body = $"Brand đã thanh toán cho đơn hàng #{booking.Id.ToString().Substring(0, 8)}. Bạn có thể bắt đầu làm việc.",
            CreatedAt = DateTime.UtcNow
        });
    }

    public IActionResult Success(Guid bookingId)
    {
        return View(new PaymentResultViewModel 
        { 
            Success = true, 
            Message = "Thanh toán thành công! KOL sẽ bắt đầu thực hiện công việc.",
            BookingId = bookingId 
        });
    }
}
