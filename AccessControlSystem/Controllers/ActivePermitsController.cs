using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class ActivePermitsController : Controller
{
    private readonly IActivePermitService _permits;
    private readonly IGuestService _guests;
    private readonly ILookupService _lookups;

    public ActivePermitsController(IActivePermitService permits, IGuestService guests, ILookupService lookups)
    {
        _permits = permits;
        _guests = guests;
        _lookups = lookups;
    }

    /// <summary>Hazırda binada olan aktiv icazələr.</summary>
    public async Task<IActionResult> Index()
    {
        var model = new ActivePermitsViewModel
        {
            Permits = await _permits.GetActiveAsync(),
            Hosts = await _lookups.GetHostsAsync(),
            Areas = await _lookups.GetAreasAsync(),
            Purposes = await _lookups.GetPurposesAsync()
        };
        return View(model);
    }

    /// <summary>Aktiv icazənin çıxışını təsdiqlə.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("active_permits", PermType.Edit)]
    public async Task<IActionResult> Checkout(long id)
    {
        try
        {
            await _guests.CheckOutAsync(id);
            TempData["Success"] = "Çıxış təsdiqləndi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }
}
