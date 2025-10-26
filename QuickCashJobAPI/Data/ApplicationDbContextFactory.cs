using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

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

            // ✅ Read environment variable (for Railway/PostgreSQL)
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

            if (!string.IsNullOrEmpty(databaseUrl))
            {
                // PostgreSQL for Railway
                var connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
                optionsBuilder.UseNpgsql(connectionString);
            }
            else
            {
                // Fallback local connection string (SQL Server or local PostgreSQL)
                var localConnectionString = configuration.GetConnectionString("DefaultConnection")
                                            ?? "Server=localhost;Database=QuickCashDb;User Id=sa;Password=YourPassword;";
                optionsBuilder.UseNpgsql(localConnectionString);
            }

            return new ApplicationDbContext(optionsBuilder.Options);
        }

        private static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':');
            return $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
        }
    }
}
