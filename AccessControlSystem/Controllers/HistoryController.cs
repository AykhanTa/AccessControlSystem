using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class HistoryController : Controller
{
    private readonly IHistoryService _history;
    private readonly ISettingsService _settings;
    public HistoryController(IHistoryService history, ISettingsService settings)
    {
        _history = history;
        _settings = settings;
    }

    /// <summary>Giriş-çıxış tarixçəsi — tarix aralığı ilə filtrlənə bilər.</summary>
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var model = new HistoryViewModel
        {
            From = from,
            To = to,
            Items = await _history.GetHistoryAsync(from, to),
            Companies = await _settings.GetCompaniesAsync()
        };
        return View(model);
    }
}
