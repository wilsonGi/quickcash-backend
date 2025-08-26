namespace QuickCashJobAPI.Models.DTO
{
    public class AdUserDTO
    {
        public string Id { get; set; } = null!;
        public string? Name { get; set; }
        public string? Location { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePhoto { get; set; }
        public int NumberOfTasksCompleted { get; set; }
        public int NumberOfTasksEmployed { get; set; }
        public DateTime? LastTaskDoneDate { get; set; }
        public DateTime? LastTaskEmployedDate { get; set; }
        public double UserRating { get; set; }
        public List<string>? Skills { get; set; }
        public List<string>? CompletedCategories { get; set; }
        public List<string>? EmployedCategories { get; set; }
    }

}
