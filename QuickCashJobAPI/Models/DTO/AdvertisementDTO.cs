namespace QuickCashJobAPI.Models.DTO
{
    public class AdvertisementDTO
    {
        // Advertisement fields
        public int Id { get; set; }

        public string Category { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;

        public bool IsSubscriptionActive { get; set; }

        // 👤 Nested User
        public AdUserDTO? User { get; set; }
    }
}
