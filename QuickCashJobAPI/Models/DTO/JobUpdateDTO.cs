using QuickCashJobAPI.Enums;

namespace QuickCashJobAPI.Models.DTO
{
    public class JobUpdateDTO
    {
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public JobStatus Status { get; set; }
        public byte[]? AudioDescription { get; set; }
        public double Payout { get; set; }
        public bool Negotiable { get; set; }
    }
}
