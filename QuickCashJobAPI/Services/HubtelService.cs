// HubtelService.cs
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace QuickCashJobAPI.Services
{
    public class HubtelService : IHubtelService
    {
        private readonly HttpClient _http;
        private readonly HubtelOptions _opt;
        private readonly ILogger<HubtelService> _logger;

        public HubtelService(HttpClient http, IOptions<HubtelOptions> opt, ILogger<HubtelService> logger)
        {
            _http = http;
            _opt = opt.Value;
            _logger = logger;

            _http.BaseAddress = new Uri(_opt.BaseUrl);
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_opt.ClientId}:{_opt.ClientSecret}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        }

        public async Task<string?> InitializeInvoiceAsync(string reference, decimal amount, string currency, string customerEmail, string callbackUrl)
        {
            var body = new
            {
                merchantAccountNumber = _opt.MerchantAccountNumber,
                invoiceReference = reference,
                invoiceAmount = amount,
                currency = currency,
                customerEmail = customerEmail,
                callbackUrl = callbackUrl
            };

            var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync("/transactions/initiate", content); // Adjust endpoint path if different
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Hubtel init failed: {Json}", json);
                return null;
            }

            using var doc = JsonDocument.Parse(json);
            // Adjust property path based on actual response
            return doc.RootElement.GetProperty("data").GetProperty("checkoutUrl").GetString();
        }

        public async Task<(string Status, string GatewayResponse)> VerifyInvoiceAsync(string reference)
        {
            var res = await _http.GetAsync($"/transactions/status/{reference}"); // Adjust endpoint path if different
            var json = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Hubtel verify failed: {Json}", json);
                return ("FAILED", json);
            }

            using var doc = JsonDocument.Parse(json);
            var status = doc.RootElement.GetProperty("data").GetProperty("status").GetString() ?? "UNKNOWN";
            var gatewayResponse = doc.RootElement.GetProperty("data").GetProperty("message").GetString() ?? "";

            return (status.ToUpperInvariant(), gatewayResponse);
        }
    }
}
