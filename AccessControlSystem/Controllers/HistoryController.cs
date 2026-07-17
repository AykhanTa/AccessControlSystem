using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class HistoryController : Controller
{
    private readonly IHistoryService _history;
    public HistoryController(IHistoryService history) => _history = history;

    /// <summary>Giriş-çıxış tarixçəsi — tarix aralığı ilə filtrlənə bilər.</summary>
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var model = new HistoryViewModel
        {
            From = from,
            To = to,
            Items = await _history.GetHistoryAsync(from, to)
        };
        return View(model);
    }
}
