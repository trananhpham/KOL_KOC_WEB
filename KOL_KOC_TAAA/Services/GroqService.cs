using System.Text;
using System.Text.Json;

namespace KOL_KOC_TAAA.Services;

public interface IGroqService
{
    Task<string> GetChatResponseAsync(string userMessage);
}

public class GroqService : IGroqService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string ApiUrl = "https://api.groq.com/openai/v1/chat/completions";

    public GroqService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey is missing");
    }

    public async Task<string> GetChatResponseAsync(string userMessage)
    {
        var requestBody = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "system", content = "Bạn là trợ lý AI thông minh của hệ thống KOL Market. Hệ thống này giúp kết nối Brand/Doanh nghiệp với KOL/KOC. Nếu user hỏi về cách liên lạc, hãy cung cấp số điện thoại 0965534645. Trả lời ngắn gọn, chuyên nghiệp và thân thiện bằng tiếng Việt." },
                new { role = "user", content = userMessage }
            },
            temperature = 0.7,
            max_tokens = 512
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ApiUrl);
        request.Headers.Add("Authorization", $"Bearer {_apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseContent);
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "Xin lỗi, tôi không thể xử lý yêu cầu lúc này.";
    }
}
