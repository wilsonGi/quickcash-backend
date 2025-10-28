namespace QuickCashJobAPI.Models.DTO
{
    public class AdvertisementDTO
    {
        // Advertisement fields
        public int Id { get; set; }

        public string Category { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;

        public bool IsSubscriptionActive => User?.IsSubscriptionActive ?? false;

        // 👤 Nested User
        public AdUserDTO? User { get; set; }
        public string? Area { get; set; }     // ✅ Added
        public string? Contact { get; set; }  // ✅ Added
        public bool IsActive { get; set; }    // ✅ Added
    }
}
