using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using QuickCashJobAPI.Services;
using System;

namespace QuickCashJobAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentController : ControllerBase
    {
        private readonly ILogger<PaymentController> _logger;
        private readonly IPaystackService _paystackService;
        private readonly ApplicationDbContext _db;
        

        public PaymentController(
            ILogger<PaymentController> logger,
            IPaystackService paystackService,
            ApplicationDbContext db)
        {
            _logger = logger;
            _paystackService = paystackService;
            _db = db;
        }

        // ✅ 1. Get PAYG Rates
        [HttpGet("payg-rates")]
        public async Task<IActionResult> GetPayAsYouGoRates()
        {
            var rates = await _db.PayAsYouGoRates
                .Where(r => r.IsActive) // ensure column exists in your model
                .Select(r => new
                {
                    r.Action,
                    r.Amount,
                    r.Description
                })
                .ToListAsync();

            return Ok(rates);
        }

        // ✅ 2. Get Subscription Plan by ID
        [HttpGet("plan/{id}")]
        public async Task<IActionResult> GetPlanById(int id)
        {
            var plan = await _db.SubscriptionPlans
                .AsNoTracking()
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Type = p.Type.ToString(),
                    SubscriptionType = p.Type.ToString(),  // <- ADD THIS LINE
                    p.Amount,
                    p.DurationDays,
                    p.Features
                })

                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return NotFound(new { message = "Plan not found" });

            return Ok(plan);
        }



        // ✅ 0. Get all active subscription plans
        [HttpGet("subscription-plans")]
        public async Task<IActionResult> GetAllSubscriptionPlans()
        {
            var plans = await _db.SubscriptionPlans
        .Where(p => p.IsActive && p.Type != SubscriptionTier.AdminForever)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    Type = p.Type.ToString(),
                    SubscriptionType = p.Type.ToString(),
                    p.Amount,
                    p.DurationDays,
                    p.Features
                })
                .ToListAsync();

            return Ok(plans);
        }



        // ✅ 3. Initialize Paystack Payment
        [HttpPost("pay-subscription")]
        public async Task<IActionResult> PaySubscription([FromBody] PaymentRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request.");

            var plan = await _db.SubscriptionPlans.FindAsync(request.PlanId);
            if (plan == null || !plan.IsActive)
                return BadRequest("Invalid or inactive subscription plan.");

            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email is required for Paystack payment.");

            // ✅ Handle free plans separately
            if (plan.Amount <= 0)
            {
                var txn = new PaystackTransaction
                {
                    Reference = Guid.NewGuid().ToString("N"),
                    UserId = request.UserId,
                    PlanId = plan.Id,
                    Amount = 0,
                    Status = "SUCCESS"
                };

                _db.PaystackTransactions.Add(txn);
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Free plan activated successfully.",
                    plan = plan.Name,
                    amount = plan.Amount,
                    reference = txn.Reference,
                    status = "SUCCESS"
                });
            }

            // ✅ Paid plan: call Paystack
            var callbackUrl = "https://quickcash-frontend.web.app/payment-success";
            var reference = Guid.NewGuid().ToString("N");

            var authorizationUrl = await _paystackService.InitializeTransactionAsync(
                reference,
                request.Email,
                plan.Amount,
                callbackUrl
            );

            if (authorizationUrl == null)
                return BadRequest(new { message = "Failed to initiate Paystack payment." });

            var paidTxn = new PaystackTransaction
            {
                Reference = reference,
                UserId = request.UserId,
                PlanId = plan.Id,
                Amount = plan.Amount,
                Status = "PENDING"
            };

            _db.PaystackTransactions.Add(paidTxn);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment initialized successfully.",
                plan = plan.Name,
                amount = plan.Amount,
                reference,
                authorizationUrl,
                status = "PENDING"
            });
        }

        // ✅ 4. Verify payment status
        [HttpGet("payment-status/{reference}")]
        public async Task<IActionResult> GetPaymentStatus(string reference)
        {
            var (status, response) = await _paystackService.VerifyTransactionAsync(reference);

            var txn = await _db.PaystackTransactions.FirstOrDefaultAsync(x => x.Reference == reference);
            if (txn != null)
            {
                txn.Status = status;
                txn.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Ok(new { reference, status, gatewayResponse = response });
        }


        // ✅ 5. Initiate PAYG Payment
        [HttpPost("payg-initiate")]
        public async Task<IActionResult> InitiatePayAsYouGoPayment([FromBody] PayAsYouGoPaymentRequest request)
        {
            if (string.IsNullOrEmpty(request.UserId) || string.IsNullOrEmpty(request.Action))
                return BadRequest("Invalid request: user or action missing.");

            var rate = await _db.PayAsYouGoRates.FirstOrDefaultAsync(r => r.Action == request.Action && r.IsActive);
            if (rate == null)
                return NotFound(new { message = $"Rate not found for action: {request.Action}" });

            if (string.IsNullOrEmpty(request.Email))
                return BadRequest("Email is required for Paystack payment.");

            var reference = Guid.NewGuid().ToString("N");
            var callbackUrl = "https://quickcash-frontend.web.app/payment-success";

            _logger.LogInformation("💰 Initiating PAYG payment for {UserId}: {Action} ({Amount} GHS)",
                request.UserId, request.Action, rate.Amount);

            var authorizationUrl = await _paystackService.InitializeTransactionAsync(
                reference,
                request.Email,
                rate.Amount,
                callbackUrl
            );

            if (authorizationUrl == null)
                return BadRequest(new { message = "Failed to initiate Paystack payment." });

            // Save PAYG transaction
            var txn = new PayAsYouGoTransaction
            {
                UserId = request.UserId,
                Action = request.Action,
                Amount = rate.Amount,
                Reference = reference,
                Status = "PENDING"
            };

            _db.PayAsYouGoTransactions.Add(txn);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "PAYG payment initialized successfully.",
                action = rate.Action,
                description = rate.Description,
                amount = rate.Amount,
                reference,
                authorizationUrl
            });
        }

        // ✅ 6. Verify PAYG Payment
        [HttpGet("payg-status/{reference}")]
        public async Task<IActionResult> VerifyPayAsYouGoPayment(string reference)
        {
            var (status, response) = await _paystackService.VerifyTransactionAsync(reference);

            var txn = await _db.PayAsYouGoTransactions.FirstOrDefaultAsync(t => t.Reference == reference);
            if (txn != null)
            {
                txn.Status = status;
                txn.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return Ok(new { reference, status, gatewayResponse = response });
        }


    }
}
