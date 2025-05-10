namespace QuickCashJobAPI.Models
{
    public class PaymentTransaction
    {
        public int Id { get; set; }
        public string ReferenceId { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } // Pending, Success, Failed
        public string ResponsePayload { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
