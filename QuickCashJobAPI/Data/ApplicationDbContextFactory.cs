using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace QuickCashJobAPI.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // PostgreSQL (for Railway/Supabase)
                var connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
                optionsBuilder.UseNpgsql(connectionString);
            }
            else
            {
                // SQL Server (for Local Development)
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                optionsBuilder.UseSqlServer(connectionString);
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        private string ConvertDatabaseUrlToConnectionString(string databaseUrl)
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');

            return $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
        }
    }
}
