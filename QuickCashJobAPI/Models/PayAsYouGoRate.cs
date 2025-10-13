namespace QuickCashJobAPI.Models
{
    public class PayAsYouGoRate
    {
        public int Id { get; set; }

        public string Action { get; set; } = string.Empty; // e.g. POST_JOB, VIEW_AD_DETAILS, APPROVE_CONTRACTOR
        public decimal Amount { get; set; }                // e.g. 10, 5, etc.
        public string Description { get; set; } = string.Empty; // Optional: human-readable description
        public bool IsActive { get; set; } = true;
    }
}
