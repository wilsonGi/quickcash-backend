// IHubtelService.cs
namespace QuickCashJobAPI.Services
{
    public interface IHubtelService
    {
        Task<string?> InitializeInvoiceAsync(string reference, decimal amount, string currency, string customerEmail, string callbackUrl);
        Task<(string Status, string GatewayResponse)> VerifyInvoiceAsync(string reference);
    }
}
