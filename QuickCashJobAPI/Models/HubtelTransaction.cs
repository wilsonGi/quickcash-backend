// HubtelTransaction.cs
namespace QuickCashJobAPI.Models
{
    public class HubtelTransaction
    {
        public int Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public int PlanId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "PENDING";  // PENDING | SUCCESS | FAILED
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public virtual SubscriptionPlan Plan { get; set; } = null!;
    }
}
