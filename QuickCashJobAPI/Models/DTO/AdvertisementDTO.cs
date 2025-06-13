namespace QuickCashJobAPI.Models.DTO
{
    public class AdvertisementDTO
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Area { get; set; }
        public string Contact { get; set; }
        public bool IsSubscriptionActive { get; set; }
      
    }

}
