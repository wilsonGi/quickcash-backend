using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuickCashJobAPI.Services
{
    public class PaystackService : IPaystackService
    {
        private readonly HttpClient _http;
        private readonly PaystackOptions _opt;
        private readonly ILogger<PaystackService> _logger;

        public PaystackService(HttpClient http, IOptions<PaystackOptions> opt, ILogger<PaystackService> logger)
        {
            _http = http;
            _opt = opt.Value;
            _logger = logger;

            _http.BaseAddress = new Uri(_opt.BaseUrl);
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.SecretKey);
        }

        public async Task<string> InitializeTransactionAsync(string reference, string email, decimal amount, string callbackUrl)
        {
            var body = new
            {
                email,
                amount = (int)Math.Round(amount * 100), // safely convert GHS → pesewas
                reference,
                callback_url = callbackUrl,
                currency = "GHS"
            };

            var res = await _http.PostAsync("/transaction/initialize",
                new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"));

            var json = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Init failed: {Json}", json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("data").GetProperty("authorization_url").GetString();
        }

        public async Task<(string Status, string GatewayResponse)> VerifyTransactionAsync(string reference)
        {
            var res = await _http.GetAsync($"/transaction/verify/{reference}");
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Verify failed: {Json}", json);
                return ("FAILED", json);
            }

            using var doc = JsonDocument.Parse(json);
            var status = doc.RootElement.GetProperty("data").GetProperty("status").GetString();
            var gatewayResponse = doc.RootElement.GetProperty("data").GetProperty("gateway_response").GetString();

            return (status?.ToUpperInvariant() == "SUCCESS" ? "SUCCESS" : status?.ToUpperInvariant(), gatewayResponse);
        }
    }
}
