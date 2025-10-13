using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using QuickCashJobAPI.Models;
using QuickCashJobAPI.Models.DTO;
using System.Buffers.Text;

namespace QuickCashJobAPI.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {

        }
        public DbSet<Job> Jobs { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<JobCommitment> JobCommitments { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Blog> blogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PaymentRequest> PaymentRequests { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<UserSkill> UserSkills { get; set; }
        public DbSet<UserCompletedCategory> UserCompletedCategories { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        public DbSet<Advertisement> Advertisements { get; set; }
        public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
        public DbSet<Payment> Payments { get; set; }

        public DbSet<PaystackTransaction> PaystackTransactions { get; set; }
        public DbSet<TrialRecord> TrialRecords { get; set; }

        public DbSet<PayAsYouGoRate> PayAsYouGoRates { get; set; }
        public DbSet<PayAsYouGoTransaction> PayAsYouGoTransactions { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            //Categories
            modelBuilder.Entity<Category>().HasData(
    new Category { Id = 1, CategoryName = "Agriculture & Farming", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 2, CategoryName = "Animal Care", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 3, CategoryName = "Architecture & Interior Design", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 4, CategoryName = "Arts & Design", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 5, CategoryName = "Automotive", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 6, CategoryName = "Beauty & Personal Care", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 7, CategoryName = "Beauty & Wellness", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 8, CategoryName = "Blockchain & Cryptocurrency", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 9, CategoryName = "Business & Consulting", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 10, CategoryName = "Cleaning & Housekeeping", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 11, CategoryName = "Consulting & Strategy", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 12, CategoryName = "Construction", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 13, CategoryName = "Customer Service", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 14, CategoryName = "Data Analysis & Data Science", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 15, CategoryName = "Delivery Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 16, CategoryName = "Domestic Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 17, CategoryName = "E-commerce & Dropshipping", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 18, CategoryName = "Education", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 19, CategoryName = "Electrical", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 20, CategoryName = "Electronics Repair", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 21, CategoryName = "Engineering", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 22, CategoryName = "Environmental Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 23, CategoryName = "Event Planning & Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 24, CategoryName = "Fashion & Apparel", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 25, CategoryName = "Fashion & Tailoring", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 26, CategoryName = "Finance & Accounting", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 27, CategoryName = "Fisheries", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 28, CategoryName = "Food Services & Catering", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 29, CategoryName = "Freelancing & Gig Work", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 30, CategoryName = "Game Development", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 31, CategoryName = "Hairdressing & Barbering", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 32, CategoryName = "Handyman Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 33, CategoryName = "Healthcare & Medicine", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 34, CategoryName = "Hospitality & Tourism", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 35, CategoryName = "Human Resources (HR)", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 36, CategoryName = "IT Support & Networking", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 37, CategoryName = "Legal", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 38, CategoryName = "Logistics & Transportation", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 39, CategoryName = "Manufacturing", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 40, CategoryName = "Marketing & Advertising", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 41, CategoryName = "Media & Entertainment", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 42, CategoryName = "Mining & Energy", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 43, CategoryName = "NGOs & Nonprofits", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 44, CategoryName = "Office & Administration", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 45, CategoryName = "Pet Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 46, CategoryName = "Pharmaceutical", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 47, CategoryName = "Photography & Videography", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 48, CategoryName = "Project Management", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 49, CategoryName = "Public Services & Government", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 50, CategoryName = "Real Estate", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 51, CategoryName = "Retail", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 52, CategoryName = "Science & Research", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 53, CategoryName = "Security Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 54, CategoryName = "Social Work", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 55, CategoryName = "Software Development", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 56, CategoryName = "Sports & Fitness", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 57, CategoryName = "Technical Writing", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 58, CategoryName = "Telecommunications", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 59, CategoryName = "Trades & Technical Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 60, CategoryName = "Translation & Localization", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 61, CategoryName = "UX/UI Design", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 62, CategoryName = "Virtual Assistance", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 63, CategoryName = "Voice Acting & Audio Services", NumberOfInstances = 0, CategoryImage = null },
    new Category { Id = 64, CategoryName = "Writing & Translation", NumberOfInstances = 0, CategoryImage = null }
);



            // Skills seed
            modelBuilder.Entity<Skill>().HasData(
    new Skill { Id = 1, Name = "Plumbing" },
    new Skill { Id = 2, Name = "Electrical Work" },
    new Skill { Id = 3, Name = "Carpentry" },
    new Skill { Id = 4, Name = "Masonry" },
    new Skill { Id = 5, Name = "Painting" },
    new Skill { Id = 6, Name = "Welding" },
    new Skill { Id = 7, Name = "Landscaping" },
    new Skill { Id = 8, Name = "Roofing" },
    new Skill { Id = 9, Name = "HVAC Repair" },
    new Skill { Id = 10, Name = "Cleaning Services" },
    new Skill { Id = 11, Name = "Cooking" },
    new Skill { Id = 12, Name = "Hairdressing" },
    new Skill { Id = 13, Name = "Barbering" },
    new Skill { Id = 14, Name = "Makeup Artistry" },
    new Skill { Id = 15, Name = "Fashion Design" },
    new Skill { Id = 16, Name = "Tailoring" },
    new Skill { Id = 17, Name = "Driving" },
    new Skill { Id = 18, Name = "Auto Mechanic" },
    new Skill { Id = 19, Name = "Photography" },
    new Skill { Id = 20, Name = "Videography" },
    new Skill { Id = 21, Name = "Graphic Design" },
    new Skill { Id = 22, Name = "UI/UX Design" },
    new Skill { Id = 23, Name = "Web Development" },
    new Skill { Id = 24, Name = "Mobile App Development" },
    new Skill { Id = 25, Name = "Software Engineering" },
    new Skill { Id = 26, Name = "Data Analysis" },
    new Skill { Id = 27, Name = "Machine Learning" },
    new Skill { Id = 28, Name = "Digital Marketing" },
    new Skill { Id = 29, Name = "Social Media Management" },
    new Skill { Id = 30, Name = "SEO Optimization" },
    new Skill { Id = 31, Name = "Translation" },
    new Skill { Id = 32, Name = "Tutoring" },
    new Skill { Id = 33, Name = "Accounting" },
    new Skill { Id = 34, Name = "Bookkeeping" },
    new Skill { Id = 35, Name = "Legal Services" },
    new Skill { Id = 36, Name = "Event Planning" },
    new Skill { Id = 37, Name = "Customer Support" },
    new Skill { Id = 38, Name = "Project Management" },
    new Skill { Id = 39, Name = "Content Writing" },
    new Skill { Id = 40, Name = "Copywriting" },
    new Skill { Id = 41, Name = "Blogging" },
    new Skill { Id = 42, Name = "Fitness Training" },
    new Skill { Id = 43, Name = "Yoga Instruction" },
    new Skill { Id = 44, Name = "Massage Therapy" },
    new Skill { Id = 45, Name = "Housekeeping" },
    new Skill { Id = 46, Name = "Security Services" },
    new Skill { Id = 47, Name = "Babysitting" },
    new Skill { Id = 48, Name = "Elderly Care" },
    new Skill { Id = 49, Name = "Pet Sitting" },
    new Skill { Id = 50, Name = "Delivery Services" },
    new Skill { Id = 51, Name = "Calligraphy" },
    new Skill { Id = 52, Name = "Jewelry Making" },
    new Skill { Id = 53, Name = "Soap Making" },
    new Skill { Id = 54, Name = "Crafts & DIY" },
    new Skill { Id = 55, Name = "Tattoo Artist" },
    new Skill { Id = 56, Name = "DJ Services" },
    new Skill { Id = 57, Name = "Music Production" },
    new Skill { Id = 58, Name = "Instrument Lessons" },
    new Skill { Id = 59, Name = "Voice Coaching" },
    new Skill { Id = 60, Name = "Singing" },
    new Skill { Id = 61, Name = "Acting" },
    new Skill { Id = 62, Name = "Baking" },
    new Skill { Id = 63, Name = "Bartending" },
    new Skill { Id = 64, Name = "Interior Decorating" },
    new Skill { Id = 65, Name = "Home Renovation" },
    new Skill { Id = 66, Name = "Pool Maintenance" },
    new Skill { Id = 67, Name = "Furniture Assembly" },
    new Skill { Id = 68, Name = "Tech Support" },
    new Skill { Id = 69, Name = "Network Installation" },
    new Skill { Id = 70, Name = "Cybersecurity" },
    new Skill { Id = 71, Name = "Cloud Computing" },
    new Skill { Id = 72, Name = "Blockchain Development" },
    new Skill { Id = 73, Name = "Game Development" },
    new Skill { Id = 74, Name = "Animation" },
    new Skill { Id = 75, Name = "3D Modeling" },
    new Skill { Id = 76, Name = "Drone Services" },
    new Skill { Id = 77, Name = "Virtual Assistance" },
    new Skill { Id = 78, Name = "Business Consulting" },
    new Skill { Id = 79, Name = "Financial Planning" },
    new Skill { Id = 80, Name = "Real Estate Agent" },
    new Skill { Id = 81, Name = "Tax Preparation" },
    new Skill { Id = 82, Name = "Medical Transcription" },
    new Skill { Id = 83, Name = "Remote Tech Support" },
    new Skill { Id = 84, Name = "Proofreading" },
    new Skill { Id = 85, Name = "Editing" },
    new Skill { Id = 86, Name = "Resume Writing" },
    new Skill { Id = 87, Name = "Career Coaching" },
    new Skill { Id = 88, Name = "Public Speaking" },
    new Skill { Id = 89, Name = "Life Coaching" },
    new Skill { Id = 90, Name = "Health Coaching" },
    new Skill { Id = 91, Name = "Nutritionist" },
    new Skill { Id = 92, Name = "Diet Planning" },
    new Skill { Id = 93, Name = "Virtual Fitness Coaching" },
    new Skill { Id = 94, Name = "Research Assistance" },
    new Skill { Id = 95, Name = "Survey Conduction" },
    new Skill { Id = 96, Name = "Market Research" },
    new Skill { Id = 97, Name = "Inventory Management" },
    new Skill { Id = 98, Name = "Sales & Lead Generation" },
    new Skill { Id = 99, Name = "Telemarketing" },
    new Skill { Id = 100, Name = "Voice-over Acting" }
);

            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FcmToken).HasMaxLength(255);
                //entity.Property(e => e.DeviceId).HasMaxLength(255);
            });


           


            modelBuilder.Entity<Advertisement>()
            .HasOne(a => a.User)
            .WithMany(u => u.Advertisements)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);



            modelBuilder.Entity<PaymentRequest>()
                .HasKey(p => p.Id); // Assuming 'Id' is your primary key

            modelBuilder.Entity<PaymentRequest>()
            .Property(p => p.Amount)
            .HasPrecision(18, 4);

            modelBuilder.Entity<PaymentTransaction>()
                .Property(p => p.Amount)
                .HasPrecision(18, 4);

            //base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<JobCommitment>()
                .HasOne(jc => jc.Job)
                .WithMany(j => j.JobCommitments)
                .HasForeignKey(jc => jc.JobId)
                .OnDelete(DeleteBehavior.NoAction); // Prevents cascade delete for jobs

            modelBuilder.Entity<JobCommitment>()
                .HasOne(jc => jc.Contractor)
                .WithMany(u => u.JobCommitments)
                .HasForeignKey(jc => jc.ContractorId)
                .OnDelete(DeleteBehavior.Cascade); // Allows cascade delete for contractors

            modelBuilder.Entity<UserSkill>()
            .HasKey(us => new { us.UserId, us.SkillId });

            modelBuilder.Entity<UserSkill>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSkills)
                .HasForeignKey(us => us.UserId);

            modelBuilder.Entity<UserSkill>()
                .HasOne(us => us.Skill)
                .WithMany(s => s.UserSkills)
                .HasForeignKey(us => us.SkillId);


            modelBuilder.Entity<PaystackTransaction>()
                .HasOne<SubscriptionPlan>(pt => pt.Plan)
                .WithMany()
                .HasForeignKey(pt => pt.PlanId);



            // Force UTC for all DateTime properties in Blog
            modelBuilder.Entity<Blog>().Property(b => b.CreatedAt)
                .HasConversion(
                    v => v,
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            modelBuilder.Entity<Blog>().Property(b => b.UpdatedAt)
                .HasConversion(
                    v => v,
                    v => v == null ? null : DateTime.SpecifyKind(v.Value, DateTimeKind.Utc));

            // Apply UTC kind conversion for all DateTime properties
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime, DateTime>(
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc),
                            v => DateTime.SpecifyKind(v, DateTimeKind.Utc)
                        ));
                    }

                    if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(new ValueConverter<DateTime?, DateTime?>(
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v,
                            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v
                        ));
                    }
                }
            }


    modelBuilder.Entity<PayAsYouGoRate>().HasData(
    new PayAsYouGoRate { Id = 1, Action = "POST_JOB", Amount = 10, Description = "Pay GHS 10 to post a job (7 days)" },
    new PayAsYouGoRate { Id = 2, Action = "POST_AD", Amount = 15, Description = "Pay GHS 15 to post an advert (15 days)" },
    new PayAsYouGoRate { Id = 3, Action = "COMMIT_JOB", Amount = 5, Description = "Pay GHS 5 to commit to a job" },
    new PayAsYouGoRate { Id = 4, Action = "VIEW_AD_DETAILS", Amount = 3, Description = "Pay GHS 3 to view advert contact details" },
    new PayAsYouGoRate { Id = 5, Action = "APPROVE_CONTRACTOR", Amount = 5, Description = "Pay GHS 5 to approve a contractor" },
    new PayAsYouGoRate { Id = 6, Action = "REACTIVATE_JOB", Amount = 8, Description = "Pay GHS 8 to reactivate an expired job" }
);




            modelBuilder.Entity<SubscriptionPlan>().HasData(
        new SubscriptionPlan
        {
            Id = 1,
            Name = "7 Days Free Trial",
            Type = SubscriptionTier.FreeTrial,
            Amount = 0,
            DurationDays = 7,
            Features = "VIEW_JOBS,POST_JOB_LIMIT_3,COMMIT_JOB_LIMIT_3,VIEW_AD_LIMIT_5,POST_AD_LIMIT_3"
        },
        new SubscriptionPlan
        {
            Id = 2,
            Name = "Subscribed User",
            Type = SubscriptionTier.Subscribed,
            Amount = 100, // GHS or USD, your choice
            DurationDays = 30,
            Features = "UNLIMITED_JOBS,UNLIMITED_ADS,VIEW_ALL_ADS,COMMIT_UNLIMITED"
        },
        new SubscriptionPlan
        {
            Id = 3,
            Name = "Pay As You Go",
            Type = SubscriptionTier.PayAsYouGo,
            Amount = 0,
            DurationDays = 0,
            Features = "PAY_PER_ACTION"
        },
        new SubscriptionPlan
        {
            Id = 4,
            Name = "Admin Forever",
            Type = SubscriptionTier.AdminForever,
            Amount = 0,
            DurationDays = 36500, // ~100 years
            Features = "ALL_ACCESS,UNLIMITED_JOBS,UNLIMITED_ADS,MANAGE_USERS,SUPER_PRIVILEGES",
            IsActive = true
        }
    );

        }
    }
}
