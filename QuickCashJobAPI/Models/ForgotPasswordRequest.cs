using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class ForgotPasswordRequest
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string ClientAppUrl { get; set; } // e.g. https://yourapp.com/reset-password
    }

}
