using System.Security.Claims;
using AccessControlSystem.Application.Interfaces.Services;

namespace AccessControlSystem.Services;

/// <summary>Cari HTTP sorğusunu icra edən istifadəçini claim-lərdən oxuyur.</summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? Principal => _http.HttpContext?.User;

    public long? UserId =>
        long.TryParse(Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;

    public string UserName =>
        Principal?.Identity?.IsAuthenticated == true
            ? (Principal.FindFirst(ClaimTypes.Name)?.Value ?? "Sistem")
            : "Sistem";

    public string? IpAddress => _http.HttpContext?.Connection?.RemoteIpAddress?.ToString();
}
