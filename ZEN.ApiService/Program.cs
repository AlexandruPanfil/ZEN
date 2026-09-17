using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ZEN.ApiService.Endpoints;
using ZEN.ApiService.Hubs;
using ZEN.Application;
using ZEN.Infrastructure;
using ZEN.Infrastructure.Services;
using ZEN.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


//builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var jwtSection = builder.Configuration.GetSection("Jwt");
builder.Services.Configure<JwtSettings>(jwtSection);

var jwtSettings = jwtSection.Get<JwtSettings>();
if (string.IsNullOrEmpty(jwtSettings?.Key))
{
    throw new InvalidOperationException("JWT Key is missing or empty in appsettings.json!");
}


// Add Infrastructure and Application layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();

// Add HttpContextAccessor for StoreContextService
builder.Services.AddHttpContextAccessor();

// Add SignalR
builder.Services.AddSignalR();

// Register background service to clean up stale Waiting sales
builder.Services.AddHostedService<StaleSaleCleanupService>();

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
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
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "ZEN POS API is running.");

// Map API endpoint groups
app.MapGroup("/api/auth").MapAuthEndpoints();
app.MapGroup("/api/products").RequireAuthorization().MapProductEndpoints();
app.MapGroup("/api/categories").RequireAuthorization().MapCategoryEndpoints();
app.MapGroup("/api/brands").RequireAuthorization().MapBrandEndpoints();
app.MapGroup("/api/taxes").RequireAuthorization().MapTaxEndpoints();
app.MapGroup("/api/discounts").RequireAuthorization().MapDiscountEndpoints();
app.MapGroup("/api/promotions").RequireAuthorization().MapPromotionEndpoints();
app.MapGroup("/api/coupons").RequireAuthorization().MapCouponEndpoints();
app.MapGroup("/api/sales").RequireAuthorization().MapSaleEndpoints();
app.MapGroup("/api/inventory").RequireAuthorization().MapInventoryEndpoints();
app.MapGroup("/api/shifts").RequireAuthorization().MapShiftEndpoints();
app.MapGroup("/api/customers").RequireAuthorization().MapCustomerEndpoints();
app.MapGroup("/api/suppliers").RequireAuthorization().MapSupplierEndpoints();
app.MapGroup("/api/reports").RequireAuthorization().MapReportEndpoints();
app.MapGroup("/api/sync").RequireAuthorization().MapSyncEndpoints();
app.MapGroup("/api/equipments").RequireAuthorization().MapEquipmentEndpoints();

// Map SignalR hub
app.MapHub<CashDeskHub>("/hubs/cashdesk").RequireAuthorization();

app.MapDefaultEndpoints();

app.Run();