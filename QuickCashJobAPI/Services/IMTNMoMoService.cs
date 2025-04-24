using System.Threading.Tasks;

namespace QuickCashJobAPI.Services
{
    public interface IMTNMoMoService
    {
        Task<bool> ProcessPayment(string phoneNumber, decimal amount, string userId);
    }
}
