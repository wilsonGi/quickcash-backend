namespace QuickCashJobAPI.Models
{
    public class JobCommitment
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public string ContractorId { get; set; } // Foreign key reference to User
        public string ContractorName { get; set; }
        public DateTime CommittedAt { get; set; }

        public virtual Job Job { get; set; }
        public virtual ApplicationUser Contractor { get; set; }
        public bool IsApproved { get; set; }  // Add this field to track approval status
        public bool IsConfirmed { get; set; }
    }

}
