// HubtelOptions.cs
namespace QuickCashJobAPI.Services
{
    public class HubtelOptions
    {
        public string MerchantAccountNumber { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.hubtel.com";  // adjust if needed
        public string CallbackUrl { get; set; } = string.Empty;         // e.g. https://jobs.splxit.com/api/hubtel/webhook
    }
}
