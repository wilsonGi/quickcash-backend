using QuickCashJobAPI.Enums;
using QuickCashJobAPI.Models.DTO;
using System;

namespace QuickCashJobAPI.Models
{
    public class Job
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public JobStatus Status { get; set; }
        public DateTime DatePosted { get; set; }
        public byte[]? AudioDescription { get; set; }
        public string Payout { get; set; } = string.Empty;
        public bool Negotiable { get; set; }

        // Fields from ApplicationUser
        public string UserName { get; set; }
        public string UserLocation { get; set; }
        public int NumberOfTasksCompleted { get; set; }
        public int NumberOfTasksEmployed { get; set; }
        public DateTime UserLastTaskDoneDate { get; set; }
        public DateTime UserLastTaskEmployedDate { get; set; }
        public double UserRating { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserId { get; set; } // Add this field
        public ApplicationUser User { get; set; } // Reference to the user who created the job
        public string? CommittedUserId { get; set; } // Add this field for committed user
                                                     // Ensure these properties exist
        public string? ContractorId { get; set; }
        public string? ContractorName { get; set; }
        public bool ShowContact { get; set; } = false;


        // New fields for storing the approval GPS location
        public double? ApprovalLatitude { get; set; }
        public double? ApprovalLongitude { get; set; }
        // Navigation property
        public virtual ICollection<JobCommitment> JobCommitments { get; set; } = new List<JobCommitment>();
    }
}
