using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class HomeController : Controller
{
    private readonly IDashboardService _dashboard;

    public HomeController(IDashboardService dashboard) => _dashboard = dashboard;

    /// <summary>Ana səhifə — statistika + son qonaqlar.</summary>
    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            Stats = await _dashboard.GetStatsAsync(),
            RecentGuests = await _dashboard.GetRecentGuestsAsync(10)
        };
        return View(model);
    }

    public IActionResult Error() => View();
}
