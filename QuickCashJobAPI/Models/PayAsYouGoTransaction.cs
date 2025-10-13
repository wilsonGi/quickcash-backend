namespace QuickCashJobAPI.Models
{
    public class PayAsYouGoTransaction
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Status { get; set; } = "PENDING"; // PENDING, SUCCESS, FAILED
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }


}
