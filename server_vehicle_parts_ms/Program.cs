using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Helpers;
using server_vehicle_parts_ms.Services.Implementation;
using server_vehicle_parts_ms.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, config) => config
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    // EF Core logs every SQL command at Information by default. Bump to Warning so the
    // console only shows actual problems (slow queries, errors) rather than every SELECT.
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console());

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(
                      "http://localhost:3000",
                      "https://client-vehicle-ms.vercel.app"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? jwtSettings["Key"];
var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.Configure<Users>(
    builder.Configuration.GetSection("Users")
);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = !string.IsNullOrEmpty(databaseUrl)
    ? BuildNpgsqlConnectionString(databaseUrl)
    : builder.Configuration.GetConnectionString("Postgres");

builder.Services.AddDbContext<AppDbContext>(
    (options) => { options.UseNpgsql(connectionString); }
);

var redisUrl = Environment.GetEnvironmentVariable("REDIS_URL");
var redisConfiguration = !string.IsNullOrEmpty(redisUrl)
    ? BuildRedisConnectionString(redisUrl)
    : builder.Configuration.GetConnectionString("Redis");

builder.Services.AddDistributedMemoryCache();
// builder.Services.AddStackExchangeRedisCache(options =>
// {
//     options.Configuration = redisConfiguration;
//     options.InstanceName = $"vpms:{builder.Environment.EnvironmentName}:";
// });
builder.Services.AddSingleton<ICacheService, CacheService>();

// Trust X-Forwarded-* from Railway's proxy so RemoteIpAddress reflects the real client.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth-strict", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var key = userId ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<SettingsService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<PartCategoryService>();
builder.Services.AddScoped<VehiclePartService>();
builder.Services.AddScoped<StockMovementService>();
builder.Services.AddScoped<PurchaseInvoiceService>();
builder.Services.AddScoped<SalesInvoiceService>();
builder.Services.AddScoped<HotDealService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<PartRequestService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<EmailVerificationService>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var emailSettings = new EmailSettings
{
    Host      = Environment.GetEnvironmentVariable("SMTP_HOST")       ?? builder.Configuration["Smtp:Host"]     ?? "",
    Port      = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT") ?? builder.Configuration["Smtp:Port"], out var p) ? p : 587,
    User      = Environment.GetEnvironmentVariable("SMTP_USER")       ?? builder.Configuration["Smtp:User"]     ?? "",
    Password  = Environment.GetEnvironmentVariable("SMTP_PASS")       ?? builder.Configuration["Smtp:Password"] ?? "",
    FromEmail = Environment.GetEnvironmentVariable("SMTP_FROM_EMAIL") ?? builder.Configuration["Smtp:User"]     ?? "dikshyantadahal10@gmail.com",
    FromName  = Environment.GetEnvironmentVariable("SMTP_FROM_NAME")  ?? "Vehicle Parts MS",
};
builder.Services.AddSingleton(emailSettings);
if (!string.IsNullOrEmpty(emailSettings.Host))
    builder.Services.AddSingleton<IEmailService, MailKitEmailService>();
else
    builder.Services.AddSingleton<IEmailService, LoggingOnlyEmailService>();
builder.Services.AddScoped<EmailJobs>();

var cloudinarySettings = new CloudinarySettings
{
    CloudName = Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME") ?? builder.Configuration["Cloudinary:CloudName"] ?? "",
    ApiKey    = Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")    ?? builder.Configuration["Cloudinary:ApiKey"]    ?? "",
    ApiSecret = Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET") ?? builder.Configuration["Cloudinary:ApiSecret"] ?? "",
    Folder    = Environment.GetEnvironmentVariable("CLOUDINARY_FOLDER")     ?? builder.Configuration["Cloudinary:Folder"]    ?? "vehicle-parts-ms",
};
builder.Services.AddSingleton(cloudinarySettings);
if (!string.IsNullOrWhiteSpace(cloudinarySettings.CloudName) &&
    !string.IsNullOrWhiteSpace(cloudinarySettings.ApiKey) &&
    !string.IsNullOrWhiteSpace(cloudinarySettings.ApiSecret))
    builder.Services.AddSingleton<IImageUploadService, CloudinaryImageUploadService>();
else
    builder.Services.AddSingleton<IImageUploadService, DisabledImageUploadService>();
builder.Services.AddScoped<ReminderJobs>();
builder.Services.AddScoped<ReminderScheduleService>();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opt => opt.UseNpgsqlConnection(connectionString),
        new PostgreSqlStorageOptions { SchemaName = "hangfire", PrepareSchemaIfNecessary = true }));
builder.Services.AddHangfireServer();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    var hasher = new PasswordHasher<Users>();

    void SeedUser(string email, string password, string name, string phone, UserRoles role)
    {
        if (!db.Users.Any(u => u.Email == email))
        {
            var user = new Users
            {
                Id              = Guid.NewGuid(),
                Email           = email,
                FullName        = name,
                Role            = role,
                PhoneNumber     = phone,
                Address         = "System Seed",
                IsActive        = true,
                // Seeded accounts skip the email-verification flow
                IsEmailVerified = true,
            };
            user.Password = hasher.HashPassword(user, password);
            db.Users.Add(user);
        }
    }

    SeedUser("anurodhprasain0011@gmail.com", "Test@123", "Main Admin", "9800000001", UserRoles.Admin);
    SeedUser("anurodh.thepaceinfosys@gmail.com", "Test@123", "Demo Staff", "9800000002", UserRoles.Staff);
    SeedUser("amritanurodh05@gmail.com", "Test@123", "Demo Customer", "9800000003", UserRoles.Customer);

    db.SaveChanges();
}

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

// Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

var hangfireUser = Environment.GetEnvironmentVariable("HANGFIRE_DASHBOARD_USER");
var hangfirePass = Environment.GetEnvironmentVariable("HANGFIRE_DASHBOARD_PASSWORD");
if (!string.IsNullOrEmpty(hangfireUser) && !string.IsNullOrEmpty(hangfirePass))
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireDashboardAuthFilter(hangfireUser, hangfirePass) }
    });
}

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();

    // Seed default schedule rows and (re)register every enabled job with Hangfire.
    // Admins can change the cadence at runtime via /api/reminder-schedules.
    var schedules = scope.ServiceProvider.GetRequiredService<ReminderScheduleService>();
    schedules.SyncFromDatabaseAsync().GetAwaiter().GetResult();
}


try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Railway/Heroku give DATABASE_URL as postgres://user:pass@host:port/db; Npgsql wants key/value form.
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    return $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
}

// REDIS_URL comes as redis://default:password@host:port (or rediss:// for TLS); StackExchange.Redis wants host:port,password=...,ssl=...
static string BuildRedisConnectionString(string redisUrl)
{
    var uri = new Uri(redisUrl);
    var password = uri.UserInfo.Split(':', 2).ElementAtOrDefault(1) ?? "";
    var ssl = uri.Scheme == "rediss";
    return $"{uri.Host}:{uri.Port},password={password},ssl={ssl.ToString().ToLowerInvariant()},abortConnect=false";
}