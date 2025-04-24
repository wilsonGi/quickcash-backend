using System.ComponentModel.DataAnnotations;

namespace QuickCashJobAPI.Models
{
    public class PaymentRequest
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Amount { get; set; }
    }
}
