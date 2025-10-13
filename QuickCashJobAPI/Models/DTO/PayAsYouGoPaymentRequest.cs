namespace QuickCashJobAPI.Models.DTO
{
    public class PayAsYouGoPaymentRequest
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
    }

}
