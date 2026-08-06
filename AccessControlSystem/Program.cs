using AccessControlSystem.Application;
using AccessControlSystem.Filters;
using AccessControlSystem.Infrastructure;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Development-də Hikvision cihazlarının serverə (real-vaxt event) çata bilməsi üçün
// yalnız localhost yox, BÜTÜN şəbəkə interfeyslərində dinlə. Brauzer yenə
// http://localhost:5082-də açılır; cihaz isə http://<PC_LAN_IP>:5082-yə çatır.
if (builder.Environment.IsDevelopment())
{
    builder.WebHost.UseUrls("http://0.0.0.0:5082");
}

// ---- Autentifikasiya (cookie) ----
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "AccessControl.Auth";
    });

// ---- Servislər (Onion qatları) ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AccessControlSystem.Application.Interfaces.Services.ICurrentUserService,
                           AccessControlSystem.Services.CurrentUserService>();
builder.Services.AddScoped<AccessControlSystem.Application.Interfaces.Services.ICurrentTenant,
                           AccessControlSystem.Services.HttpCurrentTenant>();
builder.Services.AddScoped<PermissionFilter>();
builder.Services.AddControllersWithViews(options =>
{
    // Bütün səhifələr üçün giriş tələb olunur ([AllowAnonymous] istisna)
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
    options.Filters.Add<PermissionFilter>();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Hikvision paylaşılan admin krediti (bütün cihazlar üçün) — konfiqurasiyadan.
var hikOptions = builder.Configuration.GetSection("Hikvision")
    .Get<AccessControlSystem.Application.Common.HikvisionOptions>()
    ?? new AccessControlSystem.Application.Common.HikvisionOptions();
builder.Services.AddSingleton(hikOptions);

// Cihaz hadisələrini PULL edən arxa-plan servisi (httpHosts backlog-unu keçir).
builder.Services.AddHostedService<AccessControlSystem.Services.DeviceEventPoller>();

// Ziyarət təmizləmə: gecikmə + çıxmış/vaxtı keçmişləri cihazlardan silmə (grace period).
builder.Services.AddHostedService<AccessControlSystem.Services.VisitMaintenanceService>();

// İşçini cihazlara yazan + üz yükləyən servis.
builder.Services.AddScoped<AccessControlSystem.Services.EmployeeSyncService>();

var app = builder.Build();

// ---- Verilənlər bazasını yarat/miqrasiya et və seed et ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
    var hasher = scope.ServiceProvider.GetRequiredService<AccessControlSystem.Application.Interfaces.Services.IPasswordHasher>();
    await DbSeeder.SeedIdentityAsync(db, hasher);
    await DbSeeder.SeedCentersAsync(db);
    await DbSeeder.SeedDevicesAsync(db);
    await DbSeeder.SeedAccessPointsAsync(db);
    await DbSeeder.SeedOrganizationAsync(db);
    await DbSeeder.SeedEmployeesSectionAsync(db);
    await DbSeeder.SeedAccessEventsSectionAsync(db);
    await DbSeeder.SeedSecuritySectionAsync(db);
    await DbSeeder.SeedLeaveTypesAsync(db);
    await DbSeeder.SeedTenantBackfillAsync(db);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ---- Statik fayllar üçün keş nəzarəti ----
// Development-də CSS/JS/şəkil keşlənməsin ki, dəyişikliklər dərhal görünsün.
Action<StaticFileResponseContext> noCacheInDev = ctx =>
{
    if (app.Environment.IsDevelopment())
    {
        ctx.Context.Response.Headers.CacheControl = "no-cache, no-store, must-revalidate";
        ctx.Context.Response.Headers.Pragma = "no-cache";
        ctx.Context.Response.Headers.Expires = "0";
    }
};

// wwwroot (yüklənmiş fayllar: /uploads/...)
app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = noCacheInDev });

// Frontend qovluğu — paylaşılan aktivlər (css, js, img) və hələ MVC-yə keçməmiş
// köhnə səhifələr (tarixce.html, hesabatlar.html və s.). Default fayl AÇILMIR ki,
// "/" ünvanı MVC-dəki Home səhifəsinə düşsün.
var frontendPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "qonaq-nezaret-sistemi"));
if (Directory.Exists(frontendPath))
{
    var provider = new PhysicalFileProvider(frontendPath);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = provider, OnPrepareResponse = noCacheInDev });
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
