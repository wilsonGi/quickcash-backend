namespace QuickCashJobAPI.Services
{
    public interface IPaystackService
    {
        Task<string> InitializeTransactionAsync(string reference, string email, decimal amount, string callbackUrl);
        Task<(string Status, string GatewayResponse)> VerifyTransactionAsync(string reference);
    }
}
