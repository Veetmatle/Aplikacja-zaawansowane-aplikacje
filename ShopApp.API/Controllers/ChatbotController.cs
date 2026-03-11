using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ShopApp.Application.Interfaces;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.API.Controllers;

/// <summary>
/// Chatbot endpoint (Gemini AI). 
/// Requires either JWT authentication or X-Session-Id header to prevent anonymous abuse.
/// Rate limited to 5 requests per minute.
/// </summary>
[Route("api/chatbot")]
[EnableRateLimiting("chatbot")]
public class ChatbotController : BaseController
{
    private readonly IChatbotService _chatbotService;
    private readonly ICurrentUserService _currentUser;

    public ChatbotController(IChatbotService chatbotService, ICurrentUserService currentUser)
    {
        _chatbotService = chatbotService;
        _currentUser = currentUser;
    }

    /// <summary>Ask the chatbot a question. Requires authentication or X-Session-Id header.</summary>
    /// <response code="200">Chatbot answer</response>
    /// <response code="400">Missing session ID or invalid request</response>
    /// <response code="429">Rate limit exceeded</response>
    [HttpPost("ask")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Ask([FromBody] AskRequest request, CancellationToken ct)
    {
        // Require either authenticated user or valid session ID
        var sessionId = Request.Headers["X-Session-Id"].FirstOrDefault();
        if (!_currentUser.IsAuthenticated && string.IsNullOrWhiteSpace(sessionId))
            return BadRequest(new { error = "Chatbot requires authentication or X-Session-Id header. Call POST /api/cart/session first." });

        return FromResult(await _chatbotService.AskAsync(request.Question, request.Context, ct));
    }

    public record AskRequest(string Question, string? Context = null);
}
