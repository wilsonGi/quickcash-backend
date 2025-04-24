using System.Text.Json;

namespace QuickCashJobAPI.Services
{
    public class MTNMoMoService : IMTNMoMoService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MTNMoMoService> _logger;
        private readonly string _apiKey;
        private readonly string _subscriptionKey;
        private readonly string _baseUrl;
        private readonly IConfiguration _configuration;


        public MTNMoMoService(HttpClient httpClient, IConfiguration configuration, ILogger<MTNMoMoService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["MTNMoMo:ApiKey"];
            _subscriptionKey = configuration["MTNMoMo:SubscriptionKey"];
            _baseUrl = "https://sandbox.momodeveloper.mtn.com"; // Change to live in production
        }

        public async Task<bool> ProcessPayment(string phoneNumber, decimal amount, string userId)
        {
            try
            {
                var referenceId = Guid.NewGuid().ToString(); // Unique transaction ID
                var token = await GetAccessToken();

                var paymentRequest = new
                {
                    amount = amount.ToString("F2"), // Ensure amount is formatted properly
                    currency = "GHS",
                    externalId = referenceId,
                    payer = new { partyIdType = "MSISDN", partyId = phoneNumber },
                    payerMessage = "Subscription Payment",
                    payeeNote = "QuickCash Subscription"
                };


                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                _httpClient.DefaultRequestHeaders.Add("X-Reference-Id", referenceId);
                _httpClient.DefaultRequestHeaders.Add("X-Target-Environment", "sandbox");
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

                var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/collection/v1_0/requesttopay", paymentRequest);

                if (response.IsSuccessStatusCode)
                {
                    return true; // Payment request was successful
                }

                _logger.LogError($"Payment failed: {await response.Content.ReadAsStringAsync()}");
                return false; // Payment failed
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in ProcessPayment: {ex.Message}");
                return false; // Handle failure properly
            }
        }

        private async Task<string> GetAccessToken()
        {
            try
            {
                // Retrieve values from appsettings.json
                var apiUser = _configuration["MTNMoMo:ApiUser"];
                var apiKey = _configuration["MTNMoMo:ApiKey"];

                if (string.IsNullOrEmpty(apiUser) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("API User or API Key is missing in appsettings.json.");
                    throw new Exception("Missing MoMo API credentials.");
                }

                // Encode ApiUser:ApiKey in Base64 for Basic Authentication
                var encodedCredentials = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiUser}:{apiKey}"));

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedCredentials}");
                _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

                var response = await _httpClient.PostAsync($"{_baseUrl}/collection/token/", null);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Failed to get access token: {await response.Content.ReadAsStringAsync()}");
                    throw new Exception("Could not get MoMo API access token.");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseBody);
                return jsonDoc.RootElement.GetProperty("access_token").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in GetAccessToken: {ex.Message}");
                throw;
            }
        }


    }
}