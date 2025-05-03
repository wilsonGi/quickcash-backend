namespace QuickCashJobAPI.Models.DTO
{
    public class DashboardDTO
    {
        public string UserName { get; set; }
        public int NumberOfTasksEmployed { get; set; }
        public int NumberOfTasksCompleted { get; set; }
        public double UserRating { get; set; }
        public string Location { get; set; }
      
        public DateTime LastTaskDoneDate { get; set; }
        public DateTime LastTaskEmployedDate { get; set; }
        public DateTime DateJoined { get; set; }
        public bool IsSubscriptionActive { get; set; } // Add this
        public bool IsApproved { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public string ProfilePhoto { get; set; }
        public int ChatCount { get; set; }


    }
}
