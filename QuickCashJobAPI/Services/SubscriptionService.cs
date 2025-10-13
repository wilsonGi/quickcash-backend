using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Enums;
using QuickCashJobAPI.Models;

namespace QuickCashJobAPI.Services
{
    public class SubscriptionService
    {
        private readonly ApplicationDbContext _db;

        public SubscriptionService(ApplicationDbContext db)
        {
            _db = db;
        }

        private string GetCurrentTier(ApplicationUser user, SubscriptionPlan? plan)
        {
            if (plan == null) return "ANONYMOUS";

            if (plan.Type == SubscriptionTier.FreeTrial &&
                user.TrialEndDate > DateTime.UtcNow)
                return "FREE_TRIAL";

            if (plan.Type == SubscriptionTier.Subscribed &&
                user.SubscriptionEndDate.HasValue &&
                user.SubscriptionEndDate.Value > DateTime.UtcNow)
                return "SUBSCRIBED";

            if (plan.Type == SubscriptionTier.PayAsYouGo)
                return "PAYG";

            if (plan.Type == SubscriptionTier.AdminForever)
                return "ADMIN_FOREVER";

            return "ANONYMOUS";
        }


        /// <summary>
        /// Can always view advert summaries (title/description), 
        /// but not details unless allowed
        /// </summary>
        public bool CanViewAdSummary(ApplicationUser user) =>
            !string.IsNullOrEmpty(user.Id); // must be registered

        public async Task<bool> CanPostJob(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "SUBSCRIBED") return true;

            if (tier == "ADMIN_FOREVER") return true;

            if (tier == "FREE_TRIAL")
            {
                var count = await _db.Jobs.CountAsync(j => j.UserId == user.Id &&
                                                           j.DatePosted >= user.DateJoined);
                return count < 3;
            }

            if (tier == "PAYG")
            {
                return await _db.Payments.AnyAsync(p =>
                    p.UserId == user.Id &&
                    p.Action == "POST_JOB" &&
                    p.CreatedAt > DateTime.UtcNow.AddDays(-7));
            }

            return false;
        }

        public async Task<bool> CanCommitJob(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "SUBSCRIBED") return true;

            if (tier == "ADMIN_FOREVER") return true;

            if (tier == "FREE_TRIAL")
            {
                var count = await _db.JobCommitments.CountAsync(c => c.ContractorId == user.Id &&
                                                                     c.CommittedAt >= user.DateJoined);
                return count < 3;
            }

            if (tier == "PAYG")
            {
                return await _db.Payments.AnyAsync(p =>
                    p.UserId == user.Id &&
                    p.Action == "COMMIT_JOB" &&
                    p.CreatedAt > DateTime.UtcNow.AddDays(-1));
            }

            return false;
        }

        public async Task<bool> CanPostAd(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "ADMIN_FOREVER") return true;

            if (tier == "SUBSCRIBED")
            {
                var count = await _db.Advertisements.CountAsync(a => a.UserId == user.Id &&
                                                                     a.CreatedAt >= user.SubscriptionStartDate);
                return count < 10;
            }

            if (tier == "FREE_TRIAL")
            {
                var count = await _db.Advertisements.CountAsync(a => a.UserId == user.Id &&
                                                                     a.CreatedAt >= user.DateJoined);
                return count < 3;
            }

            if (tier == "PAYG")
            {
                return await _db.Payments.AnyAsync(p =>
                    p.UserId == user.Id &&
                    p.Action == "POST_AD" &&
                    p.CreatedAt > DateTime.UtcNow.AddDays(-15));
            }

            return false;
        }

        public async Task<bool> CanViewAdDetails(ApplicationUser user, int adViewsSoFar)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "ADMIN_FOREVER") return true;

            if (tier == "SUBSCRIBED") return true;

            if (tier == "FREE_TRIAL")
                return adViewsSoFar < 5;

            if (tier == "PAYG")
            {
                return await _db.Payments.AnyAsync(p =>
                    p.UserId == user.Id &&
                    p.Action == "VIEW_AD" &&
                    p.CreatedAt > DateTime.UtcNow.AddDays(-1));
            }

            return false;
        }

        public async Task<bool> CanApproveJob(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "ADMIN_FOREVER") return true;
            if (tier == "SUBSCRIBED") return true;
            if (tier == "FREE_TRIAL") return false;

            if (tier == "PAYG")
            {
                return await _db.Payments.AnyAsync(p =>
                    p.UserId == user.Id &&
                    p.Action == "APPROVE_JOB" &&
                    p.CreatedAt > DateTime.UtcNow.AddDays(-1));
            }

            return false;
        }

        public async Task<bool> CanConfirmJob(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "ADMIN_FOREVER") return true;
            if (tier == "SUBSCRIBED") return true;
            if (tier == "FREE_TRIAL") return false; // can commit but not confirm
            if (tier == "PAYG") return true; // confirm is free after paying to commit

            return false;
        }

        public async Task<bool> CanCompleteJob(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "ADMIN_FOREVER") return true;
            if (tier == "SUBSCRIBED") return true;
            if (tier == "FREE_TRIAL") return false;
            if (tier == "PAYG") return true; // free once paid

            return false;
        }

        public async Task<bool> CanActivateJob(ApplicationUser user)
        {
            var plan = await _db.SubscriptionPlans.FindAsync(user.CurrentPlanId);
            var tier = GetCurrentTier(user, plan);

            if (tier == "ADMIN_FOREVER") return true;
            if (tier == "SUBSCRIBED") return true;
            if (tier == "FREE_TRIAL") return false;

            if (tier == "PAYG")
            {
                return await _db.Payments.AnyAsync(p =>
                    p.UserId == user.Id &&
                    p.Action == "ACTIVATE_JOB" &&
                    p.CreatedAt > DateTime.UtcNow.AddDays(-7));
            }

            return false;
        }

        public async Task SetAnonymous(ApplicationUser user)
        {
            user.CurrentPlanId = null;
            user.SubscriptionStartDate = null;
            user.SubscriptionEndDate = null;
            user.IsSubscriptionActive = false;

            var jobs = _db.Jobs.Where(j => j.UserId == user.Id);
            foreach (var job in jobs) job.Status = JobStatus.Inactive;

            var ads = _db.Advertisements.Where(a => a.UserId == user.Id);
            foreach (var ad in ads) ad.IsActive = false;

            await _db.SaveChangesAsync();
        }
    }
}
