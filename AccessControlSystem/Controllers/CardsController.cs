using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class CardsController : Controller
{
    private readonly ICardService _cards;

    public CardsController(ICardService cards) => _cards = cards;

    /// <summary>Müvəqqəti kartların siyahısı.</summary>
    public async Task<IActionResult> Index()
    {
        var model = new CardsViewModel { Cards = await _cards.GetAllAsync() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Add)]
    public async Task<IActionResult> Create(string no, string? note)
    {
        await Guard(() => _cards.CreateAsync(new CardCreateDto { No = no, Note = note }),
                    "Kart əlavə edildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Edit)]
    public async Task<IActionResult> Update(long id, string no, string? note)
    {
        await Guard(() => _cards.UpdateAsync(id, new CardUpdateDto { No = no, Note = note }),
                    "Kart yeniləndi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Edit)]
    public async Task<IActionResult> Toggle(long id)
    {
        await Guard(() => _cards.ToggleActiveAsync(id), "Kartın vəziyyəti dəyişdirildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        await Guard(() => _cards.DeleteAsync(id), "Kart silindi.");
        return RedirectToAction(nameof(Index));
    }

    private async Task Guard(Func<Task> action, string successMessage)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
    }
}
