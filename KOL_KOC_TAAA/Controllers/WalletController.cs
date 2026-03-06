using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class WalletController : Controller
{
    private readonly KolMarketplaceContext _context;

    public WalletController(KolMarketplaceContext context)
    {
        _context = context;
    }

    private Guid GetCurrentUserId()
    {
        var idString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var wallet = await _context.UserWallets
            .Include(w => w.WalletLedgers)
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
        {
            wallet = new UserWallet
            {
                UserId = userId,
                Balance = 0,
                LockedBalance = 0,
                Currency = "VND",
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserWallets.Add(wallet);
            await _context.SaveChangesAsync();
        }

        var model = new WalletIndexViewModel
        {
            Balance = wallet.Balance,
            LockedBalance = wallet.LockedBalance,
            Currency = wallet.Currency,
            Transactions = wallet.WalletLedgers.OrderByDescending(t => t.CreatedAt).Select(t => new WalletTransactionViewModel
            {
                Amount = t.Amount,
                Type = t.TransactionType,
                Description = t.Description ?? "",
                CreatedAt = t.CreatedAt
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "KOL")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayout(decimal amount, string bankInfo)
    {
        var userId = GetCurrentUserId();
        var wallet = await _context.UserWallets.FindAsync(userId);

        if (wallet == null || wallet.Balance < amount)
        {
            TempData["ErrorMessage"] = "Số dư không đủ để rút tiền.";
            return RedirectToAction(nameof(Index));
        }

        if (amount < 50000)
        {
            TempData["ErrorMessage"] = "Số tiền rút tối thiểu là 50,000 VND.";
            return RedirectToAction(nameof(Index));
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Lock funds
            wallet.Balance -= amount;
            wallet.LockedBalance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            // 2. Create Payout Request
            var payout = new PayoutRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = amount,
                Currency = wallet.Currency,
                BankInfoJson = bankInfo,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PayoutRequests.Add(payout);

            // 3. Optional: Add Ledger entry for pending payout
            _context.WalletLedgers.Add(new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletUserId = userId,
                Amount = -amount,
                TransactionType = "PayoutRequest",
                Description = $"Yêu cầu rút tiền về {bankInfo}",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Yêu cầu rút tiền đã được gửi. Chúng tôi sẽ xử lý trong vòng 24h.";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Có lỗi xảy ra, vui lòng thử lại sau.";
        }

        return RedirectToAction(nameof(Index));
    }
}
