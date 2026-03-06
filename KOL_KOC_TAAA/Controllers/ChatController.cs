using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KOL_KOC_TAAA.Data;
using KOL_KOC_TAAA.Models;
using KOL_KOC_TAAA.ViewModels;
using System.Security.Claims;

namespace KOL_KOC_TAAA.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly KolMarketplaceContext _context;

    public ChatController(KolMarketplaceContext context)
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
        var conversations = await _context.ChatMembers
            .Include(m => m.Conversation)
                .ThenInclude(c => c.ChatMessages.OrderByDescending(msg => msg.SentAt).Take(1))
            .Include(m => m.Conversation.ChatMembers)
                .ThenInclude(cm => cm.User)
            .Where(m => m.UserId == userId)
            .Select(m => new ConversationViewModel
            {
                Id = m.ConversationId,
                OtherUserName = m.Conversation.ChatMembers.FirstOrDefault(cm => cm.UserId != userId).User.FullName ?? "User",
                LastMessage = m.Conversation.ChatMessages.OrderByDescending(msg => msg.SentAt).FirstOrDefault().Content ?? "[No messages]",
                LastMessageTime = m.Conversation.ChatMessages.OrderByDescending(msg => msg.SentAt).FirstOrDefault().SentAt,
                ConversationType = m.Conversation.Type
            })
            .OrderByDescending(c => c.LastMessageTime)
            .ToListAsync();

        return View(conversations);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = GetCurrentUserId();
        var conversation = await _context.ChatConversations
            .Include(c => c.ChatMembers).ThenInclude(m => m.User)
            .Include(c => c.ChatMessages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(c => c.Id == id && c.ChatMembers.Any(m => m.UserId == userId));

        if (conversation == null) return NotFound();

        var otherUser = conversation.ChatMembers.FirstOrDefault(m => m.UserId != userId)?.User;

        var model = new ChatDetailsViewModel
        {
            ConversationId = conversation.Id,
            OtherUserName = otherUser?.FullName ?? "User",
            BookingRequestId = conversation.BookingRequestId,
            Messages = conversation.ChatMessages.Select(m => new MessageViewModel
            {
                Id = m.Id,
                SenderName = m.SenderUser?.FullName ?? "User",
                IsMe = m.SenderUserId == userId,
                Content = m.Content ?? "",
                SentAt = m.SentAt,
                MessageType = m.MessageType
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(ChatDetailsViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NewMessage)) return RedirectToAction(nameof(Details), new { id = model.ConversationId });

        var userId = GetCurrentUserId();
        var chatMember = await _context.ChatMembers.AnyAsync(m => m.ConversationId == model.ConversationId && m.UserId == userId);
        if (!chatMember) return Unauthorized();

        var message = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = model.ConversationId,
            SenderUserId = userId,
            MessageType = "text",
            Content = model.NewMessage,
            SentAt = DateTime.UtcNow
        };

        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = model.ConversationId });
    }

    [HttpGet]
    public async Task<IActionResult> StartWithBooking(Guid bookingId)
    {
        var userId = GetCurrentUserId();
        var booking = await _context.BookingRequests.Include(b => b.CustomerUser).Include(b => b.KolUser.User).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking == null) return NotFound();

        // Check if conversation already exists
        var existingConv = await _context.ChatConversations
            .FirstOrDefaultAsync(c => c.BookingRequestId == bookingId);

        if (existingConv != null) return RedirectToAction(nameof(Details), new { id = existingConv.Id });

        // Create new conversation
        var conversation = new ChatConversation
        {
            Id = Guid.NewGuid(),
            Type = "booking",
            BookingRequestId = bookingId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatConversations.Add(conversation);
        
        // Add members
        _context.ChatMembers.Add(new ChatMember { ConversationId = conversation.Id, UserId = booking.CustomerUserId });
        _context.ChatMembers.Add(new ChatMember { ConversationId = conversation.Id, UserId = booking.KolUserId });

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Details), new { id = conversation.Id });
    }
}
