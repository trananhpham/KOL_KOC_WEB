using KOL_KOC_TAAA.Services;
using Microsoft.AspNetCore.Mvc;

namespace KOL_KOC_TAAA.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AiAssistantController : ControllerBase
{
    private readonly IGroqService _groqService;

    public AiAssistantController(IGroqService groqService)
    {
        _groqService = groqService;
    }

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrEmpty(request.Message))
            return BadRequest("Message cannot be empty.");

        try
        {
            var response = await _groqService.GetChatResponseAsync(request.Message);
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
