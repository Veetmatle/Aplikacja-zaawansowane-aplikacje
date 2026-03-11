using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ShopApp.Core.Interfaces.Services;

namespace ShopApp.Infrastructure.Services;

/// <summary>
/// Przelewy24 payment gateway integration.
/// Supports sandbox (https://sandbox.przelewy24.pl) and production.
/// Docs: https://developers.przelewy24.pl/
/// </summary>
public class Przelewy24Service : IPaymentGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Przelewy24Service> _logger;
    private readonly int _merchantId;
    private readonly int _posId;
    private readonly string _crcKey;
    private readonly string _reportKey;

    public Przelewy24Service(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<Przelewy24Service> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        var section = configuration.GetSection("Przelewy24");
        _merchantId = section.GetValue("MerchantId", 0);
        _posId = section.GetValue("PosId", 0);
        _crcKey = section.GetValue("CrcKey", "test-crc-key")!;
        _reportKey = section.GetValue("ReportKey", "test-report-key")!;

        if (_posId == 0) _posId = _merchantId;
    }

    public async Task<PaymentInitResult> InitializePaymentAsync(PaymentRequest request, CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Przelewy24");
            var amountInGrosze = (int)(request.Amount * 100);

            var sign = ComputeRegisterSign(request.SessionId, _merchantId, amountInGrosze, request.Currency);

            var body = new
            {
                merchantId = _merchantId,
                posId = _posId,
                sessionId = request.SessionId,
                amount = amountInGrosze,
                currency = request.Currency,
                description = request.Description,
                email = request.CustomerEmail,
                country = "PL",
                language = "pl",
                urlReturn = request.ReturnUrl,
                urlStatus = request.NotifyUrl,
                sign
            };

            var response = await client.PostAsJsonAsync("api/v1/transaction/register", body, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("P24 register failed: {Status} {Body}", response.StatusCode, errorContent);
                return new PaymentInitResult(false, Error: $"Payment gateway error: {response.StatusCode}");
            }

            var result = await response.Content.ReadFromJsonAsync<P24RegisterResponse>(ct);
            if (result?.Data?.Token is null)
                return new PaymentInitResult(false, Error: "Payment gateway returned no token.");

            var sandbox = client.BaseAddress?.Host?.Contains("sandbox") ?? true;
            var redirectBase = sandbox ? "https://sandbox.przelewy24.pl" : "https://secure.przelewy24.pl";
            var redirectUrl = $"{redirectBase}/trnRequest/{result.Data.Token}";

            return new PaymentInitResult(true, redirectUrl, result.Data.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "P24 InitializePayment exception");
            return new PaymentInitResult(false, Error: ex.Message);
        }
    }

    public Task<bool> VerifyNotificationAsync(PaymentNotification notification, CancellationToken ct = default)
    {
        try
        {
            // Verify the CRC sign from notification
            var expectedSign = ComputeNotificationSign(
                notification.SessionId,
                notification.MerchantId,
                notification.OriginAmount,
                notification.Currency);

            var isValid = string.Equals(expectedSign, notification.Sign, StringComparison.OrdinalIgnoreCase);

            if (!isValid)
                _logger.LogWarning("P24 notification sign mismatch for session {SessionId}", notification.SessionId);

            return Task.FromResult(isValid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "P24 VerifyNotification exception");
            return Task.FromResult(false);
        }
    }

    // ── CRC Signing ──────────────────────────────────────────────────────────

    private string ComputeRegisterSign(string sessionId, int merchantId, int amount, string currency)
    {
        // SHA384("{sessionId}|{merchantId}|{amount}|{currency}|{crcKey}")
        var data = $"{sessionId}|{merchantId}|{amount}|{currency}|{_crcKey}";
        return ComputeSha384(data);
    }

    private string ComputeNotificationSign(string sessionId, int merchantId, int originAmount, string currency)
    {
        var data = $"{sessionId}|{merchantId}|{originAmount}|{currency}|{_crcKey}";
        return ComputeSha384(data);
    }

    private static string ComputeSha384(string input)
    {
        var bytes = SHA384.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    // ── P24 Response DTOs (internal) ─────────────────────────────────────────

    private sealed class P24RegisterResponse
    {
        [JsonPropertyName("data")]
        public P24RegisterData? Data { get; set; }
    }

    private sealed class P24RegisterData
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
    }
}
