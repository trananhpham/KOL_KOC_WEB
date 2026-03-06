using KOL_KOC_TAAA.Services;
using KOL_KOC_TAAA.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KOL_KOC_TAAA.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AiAssistantController : ControllerBase
{
    private readonly IGroqService _groqService;
    private readonly KolMarketplaceContext _context;

    public AiAssistantController(IGroqService groqService, KolMarketplaceContext context)
    {
        _groqService = groqService;
        _context = context;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
            return BadRequest("Message cannot be empty.");

        try
        {
            // Lấy dữ liệu thực tế từ Database để làm ngữ cảnh cho AI
            var totalKols = await _context.KolProfiles.CountAsync();
            var categories = await _context.KolCategories.Select(c => c.Name).ToListAsync();
            var minPrice = await _context.RateCardItems.AnyAsync() ? await _context.RateCardItems.MinAsync(i => i.UnitPrice) : 0;
            var avgPrice = await _context.RateCardItems.AnyAsync() ? await _context.RateCardItems.AverageAsync(i => i.UnitPrice) : 0;
            
            var systemContext = $"- Tổng số lượng Idol (KOL/KOC): {totalKols}\n" +
                                $"- Các lĩnh vực hoạt động: {string.Join(", ", categories)}\n" +
                                $"- Mức giá thấp nhất: {minPrice:N0} VND\n" +
                                $"- Mức giá trung bình: {avgPrice:N0} VND\n" +
                                "- Các tính năng nổi bật: Booking tự động, Thanh toán MoMo, Hợp đồng điện tử, Quản lý sản phẩm bàn giao.";

            var response = await _groqService.GetChatResponseAsync(request.Message, systemContext);
            return Ok(new { response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Lỗi khi xử lý yêu cầu AI: " + ex.Message });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = null!;
    }
}
