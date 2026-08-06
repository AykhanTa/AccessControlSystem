using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>İş cədvəlləri (Timetable) idarəetməsi — Parametrlərdən ayrı, öz səhifəsində.
/// İcazə "settings" bölməsindən götürülür (SectionMap: Timetables → settings).</summary>
public class TimetablesController : Controller
{
    private readonly ISettingsService _settings;
    public TimetablesController(ISettingsService settings) => _settings = settings;

    public async Task<IActionResult> Index()
    {
        ViewData["Active"] = "timetables";
        ViewData["Heading"] = "İş cədvəlləri";
        ViewData["Title"] = "İş cədvəlləri";
        ViewBag.Companies = await _settings.GetCompaniesAsync();
        return View(await _settings.GetWorkSchedulesAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> Add(WorkScheduleInputDto model) =>
        Guard(() => _settings.AddWorkScheduleAsync(model), "İş cədvəli əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> Update(long id, WorkScheduleInputDto model) =>
        Guard(() => _settings.UpdateWorkScheduleAsync(id, model), "İş cədvəli yeniləndi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> Toggle(long id) =>
        Guard(() => _settings.ToggleWorkScheduleAsync(id), "İş cədvəlinin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> Delete(long id) =>
        Guard(() => _settings.DeleteWorkScheduleAsync(id), "İş cədvəli silindi.");

    private async Task<IActionResult> Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
