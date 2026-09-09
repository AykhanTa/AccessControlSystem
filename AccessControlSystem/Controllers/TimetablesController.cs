using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>İş cədvəlləri (Timetable) əməliyyatları. Səhifə Parametrlərin "İş cədvəlləri" tabındadır —
/// bu kontroller yalnız POST-ları emal edir, Index isə həmin taba yönləndirir.
/// İcazə "settings" bölməsindən götürülür (SectionMap: Timetables → settings).</summary>
public class TimetablesController : Controller
{
    private const string Tab = "timetables";

    private readonly ISettingsService _settings;
    public TimetablesController(ISettingsService settings) => _settings = settings;

    /// <summary>Köhnə /Timetables ünvanı — Parametrlərin iş cədvəlləri tabına yönləndirilir.</summary>
    public IActionResult Index() => RedirectToAction("Index", "Settings", new { tab = Tab });

    [HttpPost, ValidateAntiForgeryToken, RequirePermission(Sections.Timetables, PermType.Add)]
    public Task<IActionResult> Add(WorkScheduleInputDto model) =>
        Guard(() => _settings.AddWorkScheduleAsync(model), "İş cədvəli əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission(Sections.Timetables, PermType.Edit)]
    public Task<IActionResult> Update(long id, WorkScheduleInputDto model) =>
        Guard(() => _settings.UpdateWorkScheduleAsync(id, model), "İş cədvəli yeniləndi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission(Sections.Timetables, PermType.Edit)]
    public Task<IActionResult> Toggle(long id) =>
        Guard(() => _settings.ToggleWorkScheduleAsync(id), "İş cədvəlinin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission(Sections.Timetables, PermType.Delete)]
    public Task<IActionResult> Delete(long id) =>
        Guard(() => _settings.DeleteWorkScheduleAsync(id), "İş cədvəli silindi.");

    private async Task<IActionResult> Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction("Index", "Settings", new { tab = Tab });
    }
}
