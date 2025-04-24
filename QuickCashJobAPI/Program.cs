using Microsoft.EntityFrameworkCore;
using QuickCashJobAPI.Data;
using Microsoft.AspNetCore.Identity;
using QuickCashJobAPI.Models;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickCashJobAPI.Services;
using QuickCashJobAPI.Helpers;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(option =>
{
    option.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});


builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IEmailSender, EmailService>();


builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim(claim => claim.Type == "IsAdmin" && claim.Value == "True")));
});


// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JWT");
var key = Encoding.ASCII.GetBytes(jwtSettings.GetValue<string>("Secret"));

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
        ValidIssuer = jwtSettings.GetValue<string>("ValidIssuer"),
        ValidAudience = jwtSettings.GetValue<string>("ValidAudience"),
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    x.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine("OnAuthenticationFailed: " + context.Exception.Message);
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            Console.WriteLine("OnTokenValidated: " + context.SecurityToken);
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();
// Read API Key from appsettings.json or environment variable
var apiKey = builder.Configuration.GetValue<string>("MTNMoMo:ApiKey") ?? Environment.GetEnvironmentVariable("MTN_MOMO_APIKEY");
var encodedApiKey = ApiHelper.GetEncodedApiKey(apiKey); // Encode it

// ✅ Print encoded API key to verify it's correct
Console.WriteLine($"Encoded API Key: {encodedApiKey}");


// Configure HTTP Client for MTN MoMo
builder.Services.AddHttpClient<IMTNMoMoService, MTNMoMoService>(client =>
{
    client.DefaultRequestHeaders.Add("Authorization", $"Basic {encodedApiKey}");
    client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", builder.Configuration["MTNMoMo:SubscriptionKey"]);
});

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<SubscriptionCheckService>();
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
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyMethod()
                   .AllowAnyHeader();
        });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseStaticFiles();  // Enable serving static files
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

app.UseCors("AllowAll");
app.UseMiddleware<SubscriptionMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// This triggers seed on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var context = services.GetRequiredService<ApplicationDbContext>();
    await SeedSuperAdmin(userManager, roleManager, context);
}
app.Run();

async Task SeedSuperAdmin(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager, ApplicationDbContext context)
{
    if (!userManager.Users.Any()) // Ensure no users exist before creating the first admin
    {
        var companyAdminEmail = Environment.GetEnvironmentVariable("COMPANY_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("COMPANY_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(companyAdminEmail) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Super Admin email or password is missing from environment variables.");
            return;
        }

        // Ensure roles exist
        if (!await roleManager.RoleExistsAsync(SD.Role_Company))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Company));

        if (!await roleManager.RoleExistsAsync(SD.Role_Admin))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));


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
            PhoneNumber = "0555179587",
            IsAdmin = true, // Set to true for admin users
            TrialEndDate = DateTime.MaxValue,
        };

        var result = await userManager.CreateAsync(adminUser, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, SD.Role_Company);
            Console.WriteLine("Super Admin created successfully!");
        }
        else
        {
            Console.WriteLine("Failed to create Super Admin:");
            foreach (var error in result.Errors)
            {
                Console.WriteLine(error.Description);
            }
        }
    }
}
