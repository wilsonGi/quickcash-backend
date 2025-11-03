// HubtelController.cs
using Microsoft.AspNetCore.Mvc;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTOs;
using QuickCashJobAPI.Services;

namespace QuickCashJobAPI.Controllers
{
    [ApiController]
    [Route("api/hubtel")]
    public class HubtelController : ControllerBase
    {
        private readonly IHubtelService _hubtelService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HubtelController> _logger;

        public HubtelController(IHubtelService hubtelService, ApplicationDbContext db, ILogger<HubtelController> logger)
        {
            _hubtelService = hubtelService;
            _db = db;
            _logger = logger;
        }

        [HttpPost("initialize")]
        public async Task<IActionResult> Initialize([FromBody] PaymentInitializeDto dto)
        {
            string reference = Guid.NewGuid().ToString("N");
            var url = await _hubtelService.InitializeInvoiceAsync(reference, dto.Amount, dto.Currency, dto.Email, dto.CallbackUrl);
            if (url == null)
                return BadRequest(new { message = "Could not initialize Hubtel payment" });

            // Save transaction in DB
            var tx = new HubtelTransaction
            {
                Reference = reference,
                UserId = dto.UserId,
                PlanId = dto.PlanId,
                Amount = dto.Amount,
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };
            _db.HubtelTransactions.Add(tx);
            await _db.SaveChangesAsync();

            return Ok(new { reference, checkoutUrl = url });
        }
    }
}
