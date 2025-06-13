namespace QuickCashJobAPI.Models
{
    public class Advertisement
    {
        public int Id { get; set; }
        public string Category { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string Area { get; set; } = null!;
        public string Contact { get; set; } = null!;
        public bool IsSubscriptionActive { get; set; }

        // 🔗 Foreign Key
        public string UserId { get; set; } = null!;

        // 👤 Navigation Property
        public ApplicationUser User { get; set; } = null!;
    }

}
