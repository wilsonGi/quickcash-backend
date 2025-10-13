using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;

namespace QuickCashJobAPI.Controllers
{
    namespace JobsApi.Controllers
    {
        [ApiController]
        [Route("api/paystack/webhook")]
        public class PaystackWebhookController : ControllerBase
        {
            private readonly ApplicationDbContext _db;
            private readonly ILogger<PaystackWebhookController> _logger;
            private readonly IEmailSender _emailSender;


            public PaystackWebhookController(ApplicationDbContext db,
                ILogger<PaystackWebhookController> logger,
                IEmailSender emailSender)
            {
                _db = db;
                _logger = logger;
                _emailSender = emailSender;

            }


            [HttpPost]
            public async Task<IActionResult> Handle([FromBody] dynamic payload)
            {
                try
                {
                    string reference = payload?.data?.reference;
                    string status = payload?.data?.status?.ToUpperInvariant();

                    if (string.IsNullOrWhiteSpace(reference) || string.IsNullOrWhiteSpace(status))
                    {
                        _logger.LogWarning("Missing reference or status from webhook");
                        return BadRequest();
                    }

                    // Find transaction
                    var tx = await _db.PaystackTransactions
                        .Include(t => t.Plan) // include the plan info
                        .FirstOrDefaultAsync(t => t.Reference == reference);

                    if (tx == null)
                    {
                        _logger.LogWarning("Transaction not found for reference: {Reference}", reference);
                        return NotFound();
                    }

                    tx.Status = status;
                    tx.UpdatedAt = DateTime.UtcNow;

                    // Update user subscription if successful
                    if (status == "SUCCESS")
                    {
                        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == tx.UserId);
                        if (user != null && tx.Plan != null)
                        {
                            user.CurrentPlanId = tx.Plan.Id;
                            user.SubscriptionStartDate = DateTime.UtcNow;

                            if (tx.Plan.DurationDays > 0)
                                user.SubscriptionEndDate = DateTime.UtcNow.AddDays(tx.Plan.DurationDays);
                            else
                                user.SubscriptionEndDate = null;

                            user.IsSubscriptionActive = true;

                            _logger.LogInformation("✅ Activated plan '{Plan}' for user {UserId}", tx.Plan.Name, user.Id);

                            // ✅ Email user
                            await _emailSender.SendEmailAsync(
                                user.Email,
                                "✅ Subscription Activated",
                                $"Hi {user.Name},<br/><br/>" +
                                $"Your payment for the <strong>{tx.Plan.Name}</strong> plan was successful. " +
                                $"Your subscription is now active until <strong>{user.SubscriptionEndDate:MMMM dd, yyyy}</strong>.<br/><br/>" +
                                $"Thank you for using QuickCashJobs!"
                            );

                            // ✅ Email admin
                            var adminEmail = Environment.GetEnvironmentVariable("COMPANY_ADMIN_EMAIL");
                            if (!string.IsNullOrEmpty(adminEmail))
                            {
                                await _emailSender.SendEmailAsync(
                                    adminEmail,
                                    $"📢 New Subscription — {user.Email}",
                                    $"User <strong>{user.Name}</strong> ({user.Email}) just subscribed to the <strong>{tx.Plan.Name}</strong> plan " +
                                    $"on <strong>{DateTime.UtcNow:MMMM dd, yyyy}</strong>.<br/>" +
                                    $"Reference: <code>{tx.Reference}</code><br/>" +
                                    $"Amount: <strong>{tx.Amount} GHS</strong>"
                                );
                            }
                        }
                        else
                        {
                            _logger.LogWarning("❌ User or plan not found when processing webhook");
                        }
                    }

                    await _db.SaveChangesAsync();

                    return Ok();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Paystack webhook failed");
                    return StatusCode(500);
                }
            }


        }
    }
}
