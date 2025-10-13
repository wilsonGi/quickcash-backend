using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class PaymentRequest
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Email { get; set; }   // ✅ Add this

        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public string SubscriptionType { get; set; }
        [Required]
        public int PlanId { get; set; }
    }
}
