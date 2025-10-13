using System;
using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        // The user who paid
        [Required]
        public string UserId { get; set; }

        // E.g. "SUBSCRIBE_PLAN_2", "POST_JOB_PAYG", "APPROVE_JOB"
        [Required]
        [MaxLength(100)]
        public string Action { get; set; }

        public decimal Amount { get; set; }

        // PENDING | SUCCESS | FAILED
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        // Gateway reference (paystack reference, etc.)
        [MaxLength(200)]
        public string? Reference { get; set; }

        // Raw response or gateway payload (optional)
        public string? ResponsePayload { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
