using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using Microsoft.EntityFrameworkCore;

namespace KOL_KOC_TAAA.Services;

public class FinanceService : IFinanceService
{
    private readonly KolMarketplaceContext _context;

    public FinanceService(KolMarketplaceContext context)
    {
        _context = context;
    }

    public async Task<UserWallet> GetOrCreateWalletAsync(Guid userId)
    {
        var wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
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
        return wallet;
    }

    public async Task<List<WalletLedger>> GetLedgerHistoryAsync(Guid userId, int limit = 20)
    {
        return await _context.WalletLedgers
            .Where(l => l.WalletUserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> ProcessPaymentAsync(Guid bookingId, Guid customerId, decimal amount)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            // In a real app, this would be called AFTER successful payment gateway callback
            booking.Status = "paid_escrow"; // Money is held by platform
            booking.UpdatedAt = DateTime.UtcNow;

            // Record in ledger (Optional: Deduct from customer wallet if they used prepaid balance)
            // For now, assume direct payment. We just need to mark booking as paid.

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<bool> ReleaseEscrowToKolAsync(Guid bookingId)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var booking = await _context.Bookings
                .Include(b => b.KolUser)
                .FirstOrDefaultAsync(b => b.Id == bookingId);
            
            if (booking == null || booking.Status != "completed") return false;

            var kolId = booking.KolUserId;
            var amountToRelease = booking.TotalAmount ?? 0;

            var wallet = await GetOrCreateWalletAsync(kolId);
            wallet.Balance += amountToRelease;
            wallet.UpdatedAt = DateTime.UtcNow;

            _context.WalletLedgers.Add(new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletUserId = kolId,
                Amount = amountToRelease,
                TransactionType = "income",
                Description = $"Thanh toán cho đơn hàng #{bookingId.ToString()[..8].ToUpper()}",
                CreatedAt = DateTime.UtcNow
            });

            booking.Status = "closed"; // Final state
            
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<PayoutRequest> RequestPayoutAsync(Guid userId, decimal amount, string bankAccountInfo)
    {
        var wallet = await GetOrCreateWalletAsync(userId);
        if (wallet.Balance < amount) throw new Exception("Insufficient balance");

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Lock the amount
            wallet.Balance -= amount;
            wallet.LockedBalance += amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var request = new PayoutRequest
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = amount,
                Currency = "VND",
                Status = "pending",
                BankName = bankAccountInfo, // Using this field for simplicity
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.PayoutRequests.Add(request);
            
            _context.WalletLedgers.Add(new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletUserId = userId,
                Amount = -amount,
                TransactionType = "payout",
                Description = "Yêu cầu rút tiền đang chờ duyệt",
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            return request;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}
