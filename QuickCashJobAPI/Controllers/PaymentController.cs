using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using QuickCashJobAPI.Services;
using QuickCashJobAPI.Models;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly IMTNMoMoService _moMoService;

        public PaymentController(IMTNMoMoService moMoService)
        {
            _moMoService = moMoService;
        }

        [HttpPost("pay-subscription")]
        public async Task<IActionResult> PaySubscription([FromBody] PaymentRequest paymentRequest)
        {
            if (paymentRequest.Amount != 50.00m)
            {
                return BadRequest("Invalid amount. Subscription fee is 50 GHS.");
            }

            var success = await _moMoService.ProcessPayment(paymentRequest.PhoneNumber, paymentRequest.Amount, paymentRequest.UserId);

            if (success)
                return Ok("Subscription activated.");

            return BadRequest("Payment failed or incorrect amount.");
        }
    }
}
