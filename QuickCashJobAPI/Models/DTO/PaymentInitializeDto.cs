namespace QuickCashJobAPI.Models.DTOs
{
    public class PaymentInitializeDto
    {
        public int PlanId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string SubscriptionType { get; set; } = string.Empty;
        // ✅ Add these new required fields
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GHS";
        public string CallbackUrl { get; set; } = string.Empty;
    }
}
