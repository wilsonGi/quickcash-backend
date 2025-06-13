using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }  // FK to ApplicationUser

        public ApplicationUser User { get; set; }

        [Required]
        [MaxLength(100)]
        public string Title { get; set; }

        [Required]
        [MaxLength(300)]
        public string Message { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        // Optional: Associate with a Job or ChatMessage if needed
        public int? JobId { get; set; }
        public Job? Job { get; set; }

        public int? ChatMessageId { get; set; }
        public ChatMessage? ChatMessage { get; set; }
    }

}
