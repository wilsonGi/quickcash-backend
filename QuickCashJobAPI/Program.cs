using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuickCashBackend.Data;
using QuickCashBackend.Helpers;
using QuickCashBackend.Models;
using QuickCashBackend.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

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

// JWT Auth config
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
});

builder.Services.AddControllers().AddNewtonsoftJson();
builder.Services.AddEndpointsApiExplorer();

var apiKey = builder.Configuration.GetValue<string>("MTNMoMo:ApiKey") ?? Environment.GetEnvironmentVariable("MTN_MOMO_APIKEY");
var encodedApiKey = ApiHelper.GetEncodedApiKey(apiKey);
Console.WriteLine($"Encoded API Key: {encodedApiKey}");

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
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

var app = builder.Build();

// Middleware pipeline
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
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ✅ SEED SUPER ADMIN
await SeedSuperAdmin(app.Services);

app.Run();

async Task SeedSuperAdmin(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!userManager.Users.Any())
    {
        var email = Environment.GetEnvironmentVariable("COMPANY_ADMIN_EMAIL");
        var password = Environment.GetEnvironmentVariable("COMPANY_ADMIN_PASSWORD");

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
        {
            Console.WriteLine("Super Admin credentials not found in environment variables.");
            return;
        }

        if (!await roleManager.RoleExistsAsync(SD.Role_Company))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Company));
        if (!await roleManager.RoleExistsAsync(SD.Role_Admin))
            await roleManager.CreateAsync(new IdentityRole(SD.Role_Admin));

        var superAdmin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            Name = "Eric Mensah",
            Location = "Ghana",
            NumberOfTasksCompleted = 0,
            NumberOfTasksEmployed = 0,
            LastTaskDoneDate = DateTime.UtcNow,
            LastTaskEmployedDate = DateTime.UtcNow,
            UserRating = 100,
            DateJoined = DateTime.UtcNow,
            PhoneNumber = "0555179587",
            IsAdmin = true,
            TrialEndDate = DateTime.MaxValue
        };

        var result = await userManager.CreateAsync(superAdmin, password);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(superAdmin, SD.Role_Company);
            Console.WriteLine("✅ Super Admin created successfully!");
        }
        else
        {
            Console.WriteLine("❌ Failed to create Super Admin:");
            foreach (var error in result.Errors)
                Console.WriteLine($"- {error.Description}");
        }
    }
}
