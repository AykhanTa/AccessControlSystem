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
builder.Services.AddScoped<PermissionFilter>();
builder.Services.AddControllersWithViews(options =>
{
    // Bütün səhifələr üçün giriş tələb olunur ([AllowAnonymous] istisna)
    options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build()));
    options.Filters.Add<PermissionFilter>();
});
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ---- Verilənlər bazasını yarat/miqrasiya et və seed et ----
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
    var hasher = scope.ServiceProvider.GetRequiredService<AccessControlSystem.Application.Interfaces.Services.IPasswordHasher>();
    await DbSeeder.SeedIdentityAsync(db, hasher);
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
