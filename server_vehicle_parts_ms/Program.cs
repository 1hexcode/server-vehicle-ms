using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using server_vehicle_parts_ms.Data;
using server_vehicle_parts_ms.Data.Entities;
using server_vehicle_parts_ms.Services.Implementation;
using server_vehicle_parts_ms.Services.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<ProfileService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<PartCategoryService>();
builder.Services.AddScoped<VehiclePartService>();
builder.Services.AddScoped<StockMovementService>();
builder.Services.AddScoped<PurchaseInvoiceService>();
builder.Services.AddScoped<SalesInvoiceService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<VehicleService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<PartRequestService>();
builder.Services.AddScoped<ReviewService>();

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
                Id          = Guid.NewGuid(),
                Email       = email,
                FullName    = name,
                Role        = role,
                PhoneNumber = phone,
                Address     = "System Seed",
                isActive    = true,
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

app.MapControllers();

app.Run();

// Railway/Heroku give DATABASE_URL as postgres://user:pass@host:port/db; Npgsql wants key/value form.
static string BuildNpgsqlConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':', 2);
    return $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true";
}