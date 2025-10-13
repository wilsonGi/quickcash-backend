namespace QuickCashJobAPI.Services
{
    public class PaystackOptions
    {
        public string SecretKey { get; set; }
        public string PublicKey { get; set; }
        public string BaseUrl { get; set; } = "https://api.paystack.co";
        public string CallbackUrl { get; set; }  // e.g. https://jobs.splxit.com/api/paystack/webhook
    }
}
