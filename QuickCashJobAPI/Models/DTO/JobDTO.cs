using QuickCashJobAPI.Enums;

namespace QuickCashJobAPI.Models.DTO
{
    public class JobDTO
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public JobStatus Status { get; set; }
        public DateTime DatePosted { get; set; }
        public byte[]? AudioDescription { get; set; }
        public string Payout { get; set; }
        public bool Negotiable { get; set; }
        public string? JobImageUrl { get; set; } // NEW


        // Fields from ApplicationUser that are safe to expose
        public string UserName { get; set; }
        public string UserLocation { get; set; }
        public int NumberOfTasksCompleted { get; set; }
        public int NumberOfTasksEmployed { get; set; }
        public DateTime UserLastTaskDoneDate { get; set; }
        public DateTime UserLastTaskEmployedDate { get; set; }
        public double UserRating { get; set; }
        public string UserPhoneNumber { get; set; }
        public bool ShowContact { get; set; }

    }
}
