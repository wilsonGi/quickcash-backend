using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QuickCashJobAPI.Data;
using QuickCashJobAPI.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickCashJobAPI.Services;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.FileProviders;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using QuickCashJobAPI.Hubs;
using Microsoft.Extensions.Options;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// ✅ Load secrets from environment variables
var emailPassword = Environment.GetEnvironmentVariable("EMAIL_PASSWORD");
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
var adminEmail = Environment.GetEnvironmentVariable("COMPANY_ADMIN_EMAIL");
var adminPassword = Environment.GetEnvironmentVariable("COMPANY_ADMIN_PASSWORD");
var mtnApiKey = Environment.GetEnvironmentVariable("MTN_API_KEY");
var mtnSubscriptionKey = Environment.GetEnvironmentVariable("MTN_SUBSCRIPTION_KEY");
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");

if (!string.IsNullOrEmpty(databaseUrl))
{
    var connectionString = ConvertDatabaseUrlToConnectionString(databaseUrl);
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString)); 

    //builder.Services.AddDbContext<ApplicationDbContext>(options =>
    //    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

builder.Services.Configure<EmailSettings>(options =>
{
    options.SmtpServer = "smtp.gmail.com";
    options.SmtpPort = 587;
    options.SenderEmail = adminEmail ?? string.Empty;
    options.SenderName = "Quick Cash";
    options.Password = emailPassword ?? string.Empty;
    options.EnableSsl = true;
});

builder.Services.Configure<MTNMoMoSettings>(options =>
{
    options.ApiKey = mtnApiKey ?? string.Empty;
    options.SubscriptionKey = mtnSubscriptionKey ?? string.Empty;
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(claim => claim.Type == "IsAdmin" && claim.Value == "True")));
});

var jwtIssuer = builder.Configuration["JWT:ValidIssuer"] ?? "https://localhost:7018";
var jwtAudience = builder.Configuration["JWT:ValidAudience"] ?? "https://localhost:7018";
var key = Encoding.ASCII.GetBytes(jwtSecret ?? throw new Exception("JWT_SECRET not set in environment variables"));

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError("❌ OnAuthenticationFailed: {Message}", context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("✅ Token successfully validated for user.");
            return Task.CompletedTask;
        }

    };
})
.AddCookie()
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    options.CallbackPath = "/signin-google";

    // Optional: log warning instead of crashing if in development
    if (string.IsNullOrEmpty(options.ClientId) || string.IsNullOrEmpty(options.ClientSecret))
    {
        throw new Exception("Google ClientId or ClientSecret is missing. Set them as environment variables.");
    }
});


builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\""
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddScoped<IEmailSender, EmailService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<SubscriptionCheckService>();
builder.Services.AddHttpClient<IMTNMoMoService, MTNMoMoService>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// Firebase setup
var firebaseKeyPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "keys", "serviceAccountKey.json");
FirebaseApp.Create(new AppOptions
{
    Credential = GoogleCredential.FromFile(firebaseKeyPath)
});

// Optional: required for Railway free plan deployment
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

app.UseCors("AllowAll");
app.UseMiddleware<SubscriptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/notificationHub");

// ✅ Run DB migration + seeding with logging
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        await SeedRolesAsync(roleManager, logger);
        await SeedSuperAdmin(userManager, roleManager, context, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ An error occurred while migrating or seeding the database.");
    }
}

app.Run();

static string ConvertDatabaseUrlToConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');

    return $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
}

static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
{
    string[] roles = { "Admin", "Customer", "Employee", "Company" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
            logger.LogInformation("✅ Role '{Role}' created.", role);
        }
    }
}

static async Task SeedSuperAdmin(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext context,
    ILogger logger)
{
    if (!userManager.Users.Any())
    {
        var companyAdminEmail = Environment.GetEnvironmentVariable("QUICKCASH_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("QUICKCASH_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(companyAdminEmail) || string.IsNullOrEmpty(password))
        {
            logger.LogWarning("⚠️ Super Admin email or password is missing from environment variables.");
            return;
        }

        var adminUser = new ApplicationUser
        {
            UserName = companyAdminEmail,
            Email = companyAdminEmail,
            Name = "Eric Mensah",
            Location = "Ghana",
            NumberOfTasksCompleted = 0,
            NumberOfTasksEmployed = 0,
            LastTaskDoneDate = DateTime.UtcNow,
            LastTaskEmployedDate = DateTime.UtcNow,
            UserRating = 100,
            DateJoined = DateTime.UtcNow,
            PhoneNumber = "+233534861417",
            IsAdmin = true,
            IsSubscriptionActive = true,
            IsApproved = true,
            TrialEndDate = DateTime.MaxValue,
        };

        var result = await userManager.CreateAsync(adminUser, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, SD.Role_Admin);
            logger.LogInformation("✅ Super Admin created successfully.");
        }
        else
        {
            logger.LogError("❌ Failed to create Super Admin:");
            foreach (var error in result.Errors)
            {
                logger.LogError("→ {Error}", error.Description);
            }
        }
    }
}
    
