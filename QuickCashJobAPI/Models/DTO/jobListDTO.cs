using QuickCashJobAPI.Enums;

namespace QuickCashJobAPI.Models.DTO
{
    public class jobListDTO
    {
        public int CategoryId { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public JobStatus Status { get; set; }
        public DateTime DatePosted { get; set; }
        public byte[]? AudioDescription { get; set; }
        public double Payout { get; set; }
        public bool Negotiable { get; set; }
        public int UserTasksEmployed { get; set; }
        public DateTime UserLastTaskEmployedDate { get; set; }
        public double UserRating { get; set; }
        public string UserPhoneNumber { get; set; }
    }
}
