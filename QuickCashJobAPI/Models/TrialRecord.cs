using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class TrialRecord
    {
        public int Id { get; set; }

        [MaxLength(256)]
        public string Email { get; set; }

        [MaxLength(255)]
        public string PhoneNumber { get; set; }

        [MaxLength(4000)]
        public string? DeviceId { get; set; }

        public DateTime UsedAt { get; set; } = DateTime.UtcNow;
    }
}
