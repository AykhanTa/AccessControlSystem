using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AccessControlSystem.Filters;

public enum PermType { View, Add, Edit, Delete }

/// <summary>Əməliyyatı yalnız cari istifadəçinin rolunda müvafiq icazə varsa icra edir.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string section, PermType perm) : base(typeof(PermissionCheckFilter))
    {
        Arguments = new object[] { section, perm };
    }
}

public class PermissionCheckFilter : IAsyncActionFilter
{
    private readonly IAuthService _auth;
    private readonly string _section;
    private readonly PermType _perm;

    public PermissionCheckFilter(IAuthService auth, string section, PermType perm)
    {
        _auth = auth;
        _section = section;
        _perm = perm;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true &&
            user.FindFirst("IsGlobalAdmin")?.Value != "true")
        {
            // Xəritəni əvvəlcə HttpContext.Items-dən götür (global filter qoyub), yoxdursa yüklə
            var map = context.HttpContext.Items[SectionMap.PermMapKey] as Dictionary<string, SectionAccessDto>;
            if (map is null && long.TryParse(user.FindFirst("RoleId")?.Value, out var roleId))
                map = await _auth.GetPermissionMapAsync(roleId);

            var access = map is not null && map.TryGetValue(_section, out var a) ? a : new SectionAccessDto();
            var ok = _perm switch
            {
                PermType.View => access.CanView,
                PermType.Add => access.CanAdd,
                PermType.Edit => access.CanEdit,
                PermType.Delete => access.CanDelete,
                _ => false
            };

            if (!ok)
            {
                if (context.Controller is Controller c)
                    c.TempData["Error"] = "Bu əməliyyat üçün icazəniz yoxdur.";
                var controllerName = context.RouteData.Values["controller"]?.ToString() ?? "Home";
                context.Result = new RedirectToActionResult("Index", controllerName, null);
                return;
            }
        }

        await next();
    }
}
