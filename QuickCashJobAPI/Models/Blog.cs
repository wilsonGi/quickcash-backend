using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class Blog
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        [Required]
        public string Content { get; set; }

        public byte[]? ImageUrl { get; set; } // URL of the uploaded image 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

}
