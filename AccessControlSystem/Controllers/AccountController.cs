using System.Security.Claims;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly IAuthService _auth;
    private readonly ISystemLogWriter _log;
    public AccountController(IAuthService auth, ISystemLogWriter log)
    {
        _auth = auth;
        _log = log;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        var result = await _auth.AuthenticateAsync(model.Email, model.Password);
        if (!result.Success || result.User is null)
        {
            model.Error = result.Error ?? "Giriş uğursuz oldu.";
            model.Password = string.Empty;
            return View(model);
        }

        var u = result.User;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new(ClaimTypes.Name, u.FullName),
            new(ClaimTypes.Email, u.Email),
            new(ClaimTypes.Role, u.RoleName),
            new("RoleId", u.RoleId.ToString()),
            new("IsGlobalAdmin", u.IsGlobalAdmin ? "true" : "false"),
            new("SeesAllCompanies", u.SeesAllCompanies ? "true" : "false"),
        };
        if (u.CompanyId is { } cid)
            claims.Add(new Claim("CompanyId", cid.ToString()));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Çıxışı loqla (istifadəçi hələ autentifikasiyalıdır)
        var name = User.Identity?.Name ?? "İstifadəçi";
        await _log.LogAsync("LOGOUT", $"{name} sistemdən çıxdı.", "user");
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();
}
