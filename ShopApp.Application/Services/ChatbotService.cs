using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using ShopApp.Application.Common;
using ShopApp.Application.Interfaces;

namespace ShopApp.Application.Services;

/// <summary>
/// Chatbot backed by Gemini 2.0 Flash.
/// Configured via GEMINI_API_KEY in .env / appsettings.
/// TODO: Load MCP knowledge base / uploaded document as system context.
/// </summary>
public class ChatbotService : IChatbotService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private const string GeminiModel = "gemini-3.0-flash";
    private const string GeminiBaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";

    public ChatbotService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("Gemini");
        _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini:ApiKey is not configured.");
    }

    public async Task<Result<string>> AskAsync(string question, string? context = null, CancellationToken ct = default)
    {
        var systemPrompt = """
            You are a helpful assistant for an online marketplace similar to OLX/Allegro.
            You help users find items, understand how the platform works, and answer questions.
            Be concise, friendly, and helpful. If you don't know something, say so.
            """;

        if (!string.IsNullOrWhiteSpace(context))
            systemPrompt += $"\n\nAdditional context from knowledge base:\n{context}";

        var requestBody = new
        {
            system_instruction = new { parts = new[] { new { text = systemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts = new[] { new { text = question } } }
            },
            generationConfig = new { temperature = 0.7, maxOutputTokens = 1024 }
        };

        var url = $"{GeminiBaseUrl}/{GeminiModel}:generateContent?key={_apiKey}";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, requestBody, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cancellationToken: ct);
            var text = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(text))
                return Result<string>.Failure("Empty response from Gemini.");

            return Result<string>.Success(text);
        }
        catch (HttpRequestException ex)
        {
            return Result<string>.Failure($"Gemini API error: {ex.Message}");
        }
    }

    // DTO for Gemini response deserialization
    private record GeminiResponse(GeminiCandidate[]? Candidates);
    private record GeminiCandidate(GeminiContent Content);
    private record GeminiContent(GeminiPart[]? Parts);
    private record GeminiPart(string Text);
}
