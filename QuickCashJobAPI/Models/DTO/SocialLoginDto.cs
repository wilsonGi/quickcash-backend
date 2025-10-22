using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models.DTO
{
    public class SocialLoginDto
    {
        [Required]
        public string Provider { get; set; } // "Google", "Apple", etc.

        [Required]
        public string IdToken { get; set; }

        public string? DeviceId { get; set; }
    }

}
