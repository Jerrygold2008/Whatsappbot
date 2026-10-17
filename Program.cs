using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using SellFlowApi.Middleware;
using SellFlowApi.Models;
using SellFlowApi.Interfaces;
using SellFlowApi.Implementation;
using SellFlowApi.Security;
using Microsoft.AspNetCore.Authorization;
using SellFlowApi.Jobs;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SellFlowApi.Data;   // <-- must match your DbContext namespace (see note below)

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var conn = cfg.GetConnectionString("Default")!;

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddControllersWithViews();
// ---- Swagger (with a Bearer token box)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer",
        BearerFormat = "JWT", In = ParameterLocation.Header,
        Description = "Paste the JWT only (no 'Bearer ' prefix)."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// ---- Database + background jobs
builder.Services.AddDbContext<SellFlowApiDbContext>(o => o.UseNpgsql(conn));
builder.Services.AddHangfire(c => c.UsePostgreSqlStorage(o => o.UseNpgsqlConnection(conn)));
builder.Services.AddHangfireServer();

// ---- Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ValidIssuer = cfg["Jwt:Issuer"], ValidAudience = cfg["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!))
    });
builder.Services.AddScoped<IAuthorizationHandler, ActiveSubscriptionHandler>();
builder.Services.AddAuthorization(o => o.AddPolicy("ActiveSubscription",
    p => p.RequireAuthenticatedUser().AddRequirements(new ActiveSubscriptionRequirement())));

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(cfg.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()));

// ---- Services (interface -> implementation)
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<ITokenEncryptor, AesTokenEncryptor>();
builder.Services.AddScoped<IBusinessService, BusinessService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IFileStorageService, DbFileStorageService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IStoreReadinessService, StoreReadinessService>();
builder.Services.AddScoped<IPaymentWebhookService, PaymentWebhookService>();
builder.Services.AddScoped<IWhatsAppProcessor, WhatsAppProcessor>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IOwnerAlertService, OwnerAlertService>();
builder.Services.AddScoped<IOwnerCommandHandler, OwnerCommandHandler>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAdminService, AdminService>();

// ---- External APIs
builder.Services.AddHttpClient<IPaymentGateway, PaystackGateway>(c =>
    c.BaseAddress = new Uri("https://api.paystack.co/"));
builder.Services.AddHttpClient<IWhatsAppClient, WhatsAppCloudClient>(c =>
    c.BaseAddress = new Uri($"https://graph.facebook.com/{cfg["WhatsApp:GraphVersion"] ?? "v21.0"}/"));

// ---- Jobs
builder.Services.AddScoped<ReservationExpiryJob>();
builder.Services.AddScoped<SubscriptionExpiryJob>();
builder.Services.AddScoped<LowStockReminderJob>();
var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();      // https://localhost:<port>/swagger
}

app.UseStaticFiles();        // serves wwwroot/uploads
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// 1. Initialize Hangfire Dashboard & Storage
app.UseHangfireDashboard("/hangfire");

// 2. Set JobStorage.Current explicitly so RecurringJob never throws
JobStorage.Current = app.Services.GetRequiredService<JobStorage>();

// 3. Register recurring jobs
RecurringJob.AddOrUpdate<ReservationExpiryJob>(
    "release-expired-reservations",
    j => j.RunAsync(),
    Cron.Minutely);

RecurringJob.AddOrUpdate<SubscriptionExpiryJob>(
    "expire-subscriptions",
    j => j.RunAsync(),
    Cron.Daily);

// Daily stock reminder to owners at 08:00 Lagos time (07:00 UTC)
RecurringJob.AddOrUpdate<LowStockReminderJob>(
    "owner-low-stock-reminder",
    j => j.RunAsync(),
    "0 7 * * *");

app.Run();

