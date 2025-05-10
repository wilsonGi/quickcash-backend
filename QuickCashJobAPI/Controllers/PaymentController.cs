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
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IMTNMoMoService moMoService, ILogger<PaymentController> logger)
        {
            _moMoService = moMoService;
            _logger = logger;
        }

        [HttpPost("pay-subscription")]
        public async Task<IActionResult> PaySubscription([FromBody] PaymentRequest paymentRequest)
        {
            if (paymentRequest.Amount != 50.00m)
            {
                _logger.LogWarning("Invalid amount received: {Amount} for user {UserId}", paymentRequest.Amount, paymentRequest.UserId);
                return BadRequest("Invalid amount. Subscription fee is 50 GHS.");
            }

            var success = await _moMoService.ProcessPayment(paymentRequest.PhoneNumber, paymentRequest.Amount, paymentRequest.UserId);

            if (success)
                return Ok("Subscription activated.");

            _logger.LogError("Payment failed for user {UserId}, phone: {PhoneNumber}, amount: {Amount}", paymentRequest.UserId, paymentRequest.PhoneNumber, paymentRequest.Amount);
            return BadRequest("Payment failed or incorrect amount.");
        }

        [HttpGet("payment-status/{referenceId}")]
        public async Task<IActionResult> GetPaymentStatus(string referenceId)
        {
            var status = await _moMoService.GetTransactionStatus(referenceId);
            if (string.IsNullOrEmpty(status))
            {
                _logger.LogError("Payment reference not found for referenceId {ReferenceId}", referenceId);
                return NotFound("Payment reference not found or failed.");
            }

            return Ok(new { referenceId, status });
        }
    }
}
