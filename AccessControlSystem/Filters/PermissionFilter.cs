using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccessControlSystem.Filters;

/// <summary>Controller adı → bölmə kodu xəritəsi (bütün icazə yoxlamaları üçün ortaq).</summary>
public static class SectionMap
{
    public static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Home"] = "dashboard",
        ["Guests"] = "guests",
        ["Employees"] = "employees",
        ["AccessEvents"] = "access_events",
        ["Cards"] = "cards",
        ["History"] = "history",
        ["ActivePermits"] = "active_permits",
        ["Reports"] = "reports",
        ["Logs"] = "logs",
        ["Users"] = "users",
        ["Roles"] = "roles",
        ["Settings"] = "settings",
    };

    public const string PermMapKey = "PermMap";
}

/// <summary>
/// Autentifikasiya olunmuş istifadəçinin rol icazələrini yükləyir:
///  • Bütün icazə xəritəsini HttpContext.Items-ə qoyur (əməliyyat yoxlamaları üçün),
///  • View-lara sidebar və düymələr üçün icazələri ötürür,
///  • Cari bölməyə BAXIŞ icazəsi yoxdursa, Ana səhifəyə yönləndirir.
/// Global administrator bütün bölmələri və əməliyyatları görür.
/// </summary>
public class PermissionFilter : IAsyncActionFilter
{
    private readonly IAuthService _auth;
    public PermissionFilter(IAuthService auth) => _auth = auth;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "";
        SectionMap.Map.TryGetValue(controllerName, out var section);

        if (user.Identity?.IsAuthenticated == true && context.Controller is Controller controller)
        {
            var isGlobalAdmin = user.FindFirst("IsGlobalAdmin")?.Value == "true";
            controller.ViewData["IsGlobalAdmin"] = isGlobalAdmin;

            if (isGlobalAdmin)
            {
                // Hər şeyə icazəli
                controller.ViewData["AllowedSections"] = new HashSet<string>(SectionMap.Map.Values);
                SetAccess(controller, SectionAccessDto.All());
            }
            else if (long.TryParse(user.FindFirst("RoleId")?.Value, out var roleId))
            {
                var map = await _auth.GetPermissionMapAsync(roleId);
                context.HttpContext.Items[SectionMap.PermMapKey] = map;

                var allowed = map.Where(kv => kv.Value.CanView).Select(kv => kv.Key).ToHashSet();
                controller.ViewData["AllowedSections"] = allowed;

                var access = section is not null && map.TryGetValue(section, out var a) ? a : new SectionAccessDto();
                SetAccess(controller, access);

                // Baxış icazəsi yoxdursa bölməyə buraxma (Ana səhifə həmişə açıqdır)
                if (section is not null && section != "dashboard" && !access.CanView)
                {
                    controller.TempData["Error"] = "Bu bölməyə giriş icazəniz yoxdur.";
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }
        }

        await next();
    }

    private static void SetAccess(Controller c, SectionAccessDto a)
    {
        c.ViewData["CanView"] = a.CanView;
        c.ViewData["CanAdd"] = a.CanAdd;
        c.ViewData["CanEdit"] = a.CanEdit;
        c.ViewData["CanDelete"] = a.CanDelete;
    }
}
