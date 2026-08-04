using System.Security.Claims;
using AccessControlSystem.Application.Interfaces.Services;

namespace AccessControlSystem.Services;

/// <summary>
/// ICurrentTenant-in HTTP implementasiyası — şirkət kontekstini cari istifadəçinin
/// claim-lərindən (CompanyId, IsGlobalAdmin) oxuyur. Autentifikasiya yoxdursa
/// (background/seeding/login) sistem kimi davranır → təcrid tətbiq olunmur.
/// </summary>
public class HttpCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _ctx;
    public HttpCurrentTenant(IHttpContextAccessor ctx) => _ctx = ctx;

    private ClaimsPrincipal? User => _ctx.HttpContext?.User;

    public bool IsGlobalAdmin
    {
        get
        {
            var u = User;
            // HTTP konteksti/autentifikasiya yoxdursa → sistem (filtrsiz). Login sorğusu da bura düşür.
            if (u?.Identity?.IsAuthenticated != true) return true;
            return u.FindFirst("IsGlobalAdmin")?.Value == "true";
        }
    }

    public bool CanSeeAllCompanies
    {
        get
        {
            var u = User;
            if (u?.Identity?.IsAuthenticated != true) return true;   // background/seed → filtrsiz
            // Qlobal admin VƏ YA təhlükəsizlik məsulu bütün müəssisələri görür.
            return u.FindFirst("SeesAllCompanies")?.Value == "true"
                   || u.FindFirst("IsGlobalAdmin")?.Value == "true";
        }
    }

    public long? CompanyId =>
        long.TryParse(User?.FindFirst("CompanyId")?.Value, out var id) ? id : null;
}
