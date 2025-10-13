namespace QuickCashJobAPI.Models
{
    public class SubscriptionPlan
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SubscriptionTier Type { get; set; } // Enum (FreeTrial, Subscribed, PayAsYouGo, Anonymous)
        public decimal Amount { get; set; }
        public int DurationDays { get; set; } // e.g. 7 for trial, 30 for monthly
        public string Features { get; set; } // Comma-separated: "POST_JOB,VIEW_ADS"
        public bool IsActive { get; set; } = true;
    }

}
