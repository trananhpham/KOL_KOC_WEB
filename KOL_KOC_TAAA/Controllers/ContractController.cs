using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class ContractController : Controller
{
    private readonly KolMarketplaceContext _context;

    public ContractController(KolMarketplaceContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    [Authorize(Roles = "KOL")]
    public async Task<IActionResult> Draft(Guid bookingId)
    {
        var booking = await _context.Bookings
            .Include(b => b.KolUser.User)
            .Include(b => b.CustomerUser)
            .Include(b => b.BookingItems)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null) return NotFound();

        var model = new ContractDraftViewModel
        {
            BookingId = booking.Id,
            KolName = booking.KolUser.User.FullName ?? "KOL",
            BrandName = booking.CustomerUser.FullName ?? "Brand",
            TotalAmount = booking.TotalAmount,
            Currency = booking.Currency,
            TermsText = $"ĐIỀU KHOẢN HỢP ĐỒNG\n\n1. Bên A (Brand): {booking.CustomerUser.FullName}\n2. Bên B (KOL): {booking.KolUser.User.FullName}\n\nNội dung: Thực hiện quảng bá cho nhãn hàng thông qua các nền tảng social media...\n\nSản phẩm bàn giao: {string.Join(", ", booking.BookingItems.Select(i => i.ServiceType))}\n\nTổng giá trị: {booking.TotalAmount:N0} {booking.Currency}"
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "KOL")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Draft(ContractDraftViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = GetCurrentUserId();
        var booking = await _context.Bookings.FindAsync(model.BookingId);
        if (booking == null) return NotFound();

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            BookingId = model.BookingId,
            Version = 1,
            Title = model.Title,
            TermsText = model.TermsText,
            Status = "pending",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Contracts.Add(contract);
        
        booking.Status = "awaiting_signature";
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = contract.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = GetCurrentUserId();
        var contract = await _context.Contracts
            .Include(c => c.Booking)
            .Include(c => c.ContractSignatures).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (contract == null) return NotFound();

        var model = new ContractDetailsViewModel
        {
            ContractId = contract.Id,
            Title = contract.Title,
            TermsText = contract.TermsText,
            Status = contract.Status,
            TotalAmount = contract.Booking.TotalAmount,
            Signatures = contract.ContractSignatures.Select(s => new SignatureViewModel
            {
                UserName = s.User.FullName ?? s.User.Email,
                SignedAt = s.SignedAt,
                Role = s.UserId == contract.Booking.KolUserId ? "KOL" : "Brand"
            }).ToList(),
            CanSign = !contract.ContractSignatures.Any(s => s.UserId == userId) && contract.Status != "signed"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sign(Guid id)
    {
        var userId = GetCurrentUserId();
        var contract = await _context.Contracts.Include(c => c.Booking).FirstOrDefaultAsync(c => c.Id == id);
        if (contract == null) return NotFound();

        // Check if already signed
        if (await _context.ContractSignatures.AnyAsync(s => s.ContractId == id && s.UserId == userId))
        {
            return BadRequest("Bạn đã ký hợp đồng này rồi.");
        }

        var signature = new ContractSignature
        {
            Id = Guid.NewGuid(),
            ContractId = id,
            UserId = userId,
            SignedAt = DateTime.UtcNow,
            SignatureData = "DIGITAL_SIGNATURE_" + userId.ToString().Substring(0, 8) // Correct property name
        };

        _context.ContractSignatures.Add(signature);
        await _context.SaveChangesAsync();

        // Check if all parties signed
        var signaturesCount = await _context.ContractSignatures.CountAsync(s => s.ContractId == id);
        if (signaturesCount >= 2)
        {
            contract.Status = "signed";
            contract.Booking.Status = "active_contract";
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Details), new { id = id });
    }
}
