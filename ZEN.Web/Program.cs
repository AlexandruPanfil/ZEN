using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using MudBlazor.Services;
using ZEN.Application;
using ZEN.Infrastructure;
using ZEN.Infrastructure.Identity;
using ZEN.Infrastructure.Persistence;
using ZEN.Infrastructure.Persistence.Seeders;
using ZEN.Shared;
using ZEN.Web.Components;
using ZEN.Web.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// For standalone development without Docker, use in-memory cache
// To run with Docker (AppHost), use Redis cache
// Redis output cache with resilience settings
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("WarehouseInventory", builder => builder.Expire(TimeSpan.FromMinutes(2)));
});

if (builder.Environment.IsDevelopment())
{
    // In-memory cache for standalone development
    builder.Services.AddOutputCache();
}
else
{
    // Redis cache for production (via Aspire)
    builder.AddRedisOutputCache("cache");
}

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();

// Add Infrastructure and Application layers
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplication();
builder.Services.AddSharedComponents();

// MudBlazor
builder.Services.AddMudServices();

// Localization
builder.Services.AddLocalization();
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en"),
        new CultureInfo("ro"),
        new CultureInfo("ru")
    };
    options.DefaultRequestCulture = new RequestCulture("en");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
    options.SetDefaultCulture("en");
    var cookieProvider = options.RequestCultureProviders.FirstOrDefault(i => i is CookieRequestCultureProvider);
    if (cookieProvider != null)
    {
        options.RequestCultureProviders.Remove(cookieProvider);
        options.RequestCultureProviders.Insert(0, cookieProvider);
    }
});

// HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Add Session support for StoreContext
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Cookie Authentication
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.LoginPath = "/login";
//        options.LogoutPath = "/logout";
//        options.ExpireTimeSpan = TimeSpan.FromHours(8);
//        options.SlidingExpiration = true;
//    });

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddAuthorization();

builder.Services.AddCascadingAuthenticationState();

// Add SignalR service
builder.Services.AddScoped<SignalRService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SignalRService>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    
    // In Aspire, the API service URL is provided via service discovery
    // The environment variable format is: services__<service-name>__<protocol>__<port-index>
    var apiBaseUrl = configuration["services__apiservice__https__0"] 
                  ?? configuration["services__apiservice__http__0"]
                  ?? configuration["ApiService:BaseUrl"] 
                  ?? configuration["ApiService__BaseUrl"] 
                  ?? "https://localhost:7000";
    
    return new SignalRService(apiBaseUrl, logger);
});

builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        options.RootDirectory = "/Pages";
    });

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = scope.ServiceProvider.GetRequiredService<ZenDbContext>();
    await context.Database.MigrateAsync();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
    await DbSeeder.SeedAsync(context, userManager, roleManager);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRequestLocalization();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapRazorPages();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();