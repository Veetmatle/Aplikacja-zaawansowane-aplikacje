using Microsoft.AspNetCore.Mvc;
using ShopApp.Application.Interfaces;

namespace ShopApp.API.Controllers;

[Route("api/chatbot")]
public class ChatbotController : BaseController
{
    private readonly IChatbotService _chatbotService;

    public ChatbotController(IChatbotService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpPost("ask")]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
        => FromResult(await _chatbotService.AskAsync(request.Question, request.Context, ct));

    public record AskRequest(string Question, string? Context = null);
}
