namespace QuickCashJobAPI.Models
{
    public class PaystackTransaction
    {
        public int Id { get; set; }
        public string Reference { get; set; }   // Unique Paystack reference
        public string UserId { get; set; }      // FK to your user
        public int PlanId { get; set; }         // FK to SubscriptionPlan
        public decimal Amount { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING | SUCCESS | FAILED
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual SubscriptionPlan Plan { get; set; }

    }
}
