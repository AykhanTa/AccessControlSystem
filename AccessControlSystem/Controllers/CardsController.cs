using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>Müvəqqəti kartlar. Səhifə Parametrlərin "Kartlar" tabındadır —
/// bu kontroller yalnız POST-ları emal edir, Index isə həmin taba yönləndirir.</summary>
public class CardsController : Controller
{
    private const string Tab = "cards";

    private readonly ICardService _cards;

    public CardsController(ICardService cards) => _cards = cards;

    /// <summary>Köhnə /Cards ünvanı — Parametrlərin kartlar tabına yönləndirilir.</summary>
    public IActionResult Index() => Back();

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Add)]
    public async Task<IActionResult> Create(string no, string? note)
    {
        await Guard(() => _cards.CreateAsync(new CardCreateDto { No = no, Note = note }),
                    "Kart əlavə edildi.");
        return Back();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Edit)]
    public async Task<IActionResult> Update(long id, string no, string? note)
    {
        await Guard(() => _cards.UpdateAsync(id, new CardUpdateDto { No = no, Note = note }),
                    "Kart yeniləndi.");
        return Back();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Edit)]
    public async Task<IActionResult> Toggle(long id)
    {
        await Guard(() => _cards.ToggleActiveAsync(id), "Kartın vəziyyəti dəyişdirildi.");
        return Back();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("cards", PermType.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        await Guard(() => _cards.DeleteAsync(id), "Kart silindi.");
        return Back();
    }

    private IActionResult Back() => RedirectToAction("Index", "Settings", new { tab = Tab });

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
