namespace QuickCashJobAPI.Models.DTO
{
    public class ContractorDTO
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string GPSAddress { get; set; }
        public int NumberOfTasksCompleted { get; set; }
        public int NumberOfTasksEmployed { get; set; }
        public DateTime? LastTaskDoneDate { get; set; }
        public DateTime? LastTaskEmployedDate { get; set; }
        public double UserRating { get; set; }
        public string NationalIdNo { get; set; }
        public DateTime DateJoined { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsApproved { get; set; }
        public bool IsAdmin { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime? TrialEndDate { get; set; }
        public bool IsSubscriptionActive { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public List<string> Skills { get; set; }
    }

}
