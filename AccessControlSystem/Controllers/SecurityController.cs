using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>
/// Təhlükəsizlik idarəsi — nəzarət lövhəsi (oxu-yönümlü). Təhlükəsizlik məsulu rolu bütün
/// müəssisələri görür (CanSeeAllCompanies), ona görə burada bütün müəssisələrin binada olan
/// işçi + qonaqları göstərilir. Bölmə icazəsi PermissionFilter ilə yoxlanır (yalnız view).
/// </summary>
public class SecurityController : Controller
{
    private readonly IEmployeeService _employees;
    private readonly IActivePermitService _activePermits;
    private readonly ISettingsService _settings;

    public SecurityController(IEmployeeService employees, IActivePermitService activePermits, ISettingsService settings)
    {
        _employees = employees;
        _activePermits = activePermits;
        _settings = settings;
    }

    public async Task<IActionResult> Index() => View(await BuildAsync());

    /// <summary>Canlı yeniləmə (JSON) — polling ilə çağırılır.</summary>
    [HttpGet]
    public async Task<IActionResult> Feed()
    {
        var m = await BuildAsync();
        return Json(new
        {
            employees = m.Employees.Select(e => new
            {
                e.Name, e.EmployeeNo, e.Company, e.Department, e.Position, e.Presence, e.LastSeen
            }),
            guests = m.Guests.Select(g => new
            {
                g.Name, g.Company, g.Host, g.Floor, g.Status, g.Entry, g.Exit
            })
        });
    }

    private async Task<SecurityDashboardViewModel> BuildAsync()
    {
        var companies = await _settings.GetCompaniesAsync();
        var map = companies.ToDictionary(c => c.Id, c => c.Name);

        // Binada/mərtəbədə olan işçilər (bütün müəssisələr — sees-all).
        var employees = (await _employees.GetAllAsync())
            .Where(e => e.Presence is "in" or "onfloor")
            .ToList();

        // Aktiv qonaqlar (binadadır) + müəssisə adını həll et.
        var guests = await _activePermits.GetActiveAsync();
        foreach (var g in guests)
            g.Company = g.CompanyId is { } cid && map.TryGetValue(cid, out var nm) ? nm : "—";

        return new SecurityDashboardViewModel { Employees = employees, Guests = guests, Companies = companies };
    }
}
