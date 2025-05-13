using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; }
        public byte[]? ProfilePhoto { get; set; }

        [Required]
        [MaxLength(100)]
        public string Location { get; set; }


        [Required]
        public int NumberOfTasksCompleted { get; set; } = 0;

        [Required]
        public int NumberOfTasksEmployed { get; set; } = 0;

        [Required]
        public DateTime LastTaskDoneDate { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastTaskEmployedDate { get; set; } = DateTime.UtcNow;


        [Range(0, 100)]
        public double UserRating { get; set; } = 0;

        public virtual ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();


        [Required]
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;

        public bool IsDeleted { get; set; } = false;

        public bool IsBlocked { get; set; } = false; // New property to indicate if the user is blocked
        public bool IsApproved { get; set; }
        public bool IsAdmin { get; set; } = false;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime TrialEndDate { get; set; }
        public bool IsSubscriptionActive { get; set; }
        public string? DeviceId { get; set; }


        // Navigation property
        public virtual ICollection<JobCommitment> JobCommitments { get; set; } = new List<JobCommitment>();

    }
}
