using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Services;

namespace QuickCashJobAPI.Controllers
{
    namespace JobsApi.Controllers
    {
        [ApiController]
        [Route("api/subscription")]
        public class SubscriptionApiController : ControllerBase
        {
            private readonly ApplicationDbContext _db;
            private readonly IPaystackService _paystack;
            private readonly PaystackOptions _opt;

            public SubscriptionApiController(ApplicationDbContext db, IPaystackService paystack, IOptions<PaystackOptions> opt)
            {
                _db = db;
                _paystack = paystack;
                _opt = opt.Value;
            }


            [HttpPost("pay")]
            public async Task<IActionResult> Pay([FromBody] PayRequestModel model)
            {
                var plan = await _db.SubscriptionPlans.FindAsync(model.PlanId);
                if (plan == null || !plan.IsActive) return NotFound();

                if (plan.Amount == 0)
                {
                    // ✅ Handle free plan activation without Paystack
                    var tx = new PaystackTransaction
                    {
                        UserId = model.UserId,
                        PlanId = plan.Id,
                        Amount = 0,
                        Reference = Guid.NewGuid().ToString("N"),
                        Status = "SUCCESS",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _db.PaystackTransactions.Add(tx);
                    await _db.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Free plan activated",
                        reference = tx.Reference,
                        status = tx.Status,
                        plan = new { plan.Id, plan.Name, plan.DurationDays }
                    });
                }

                // ✅ Paid plans — continue with Paystack
                var paidTx = new PaystackTransaction
                {
                    UserId = model.UserId,
                    PlanId = plan.Id,
                    Amount = plan.Amount,
                    Reference = Guid.NewGuid().ToString("N"),
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                _db.PaystackTransactions.Add(paidTx);
                await _db.SaveChangesAsync();

                var initUrl = await _paystack.InitializeTransactionAsync(
                    paidTx.Reference,
                    model.Email,
                    paidTx.Amount,
                    $"{_opt.CallbackUrl}?reference={paidTx.Reference}"
                );

                if (string.IsNullOrEmpty(initUrl))
                    return BadRequest(new { error = "Unable to start payment" });

                return Ok(new { paymentUrl = initUrl, reference = paidTx.Reference });
            }




            [HttpGet("status/{reference}")]
            public async Task<IActionResult> Status(string reference)
            {
                var tx = await _db.PaystackTransactions.FirstOrDefaultAsync(t => t.Reference == reference);
                if (tx == null) return NotFound();

                if (tx.Status == "PENDING")
                {
                    var verify = await _paystack.VerifyTransactionAsync(reference);
                    tx.Status = verify.Status;
                    tx.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }

                return Ok(new { status = tx.Status, reference = tx.Reference });
            }

            [HttpGet("api/subscription/plans")]
            public async Task<IActionResult> GetPlans()
            {
                var plans = await _db.SubscriptionPlans.Where(p => p.IsActive).ToListAsync();
                return Ok(plans);
            }

        }

        public class PayRequestModel
        {
            public int PlanId { get; set; }
            public string UserId { get; set; }
            public string Email { get; set; }

        }

    }
}
