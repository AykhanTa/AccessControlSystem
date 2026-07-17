using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class SettingsController : Controller
{
    private readonly ISettingsService _settings;
    public SettingsController(ISettingsService settings) => _settings = settings;

    public async Task<IActionResult> Index()
    {
        var model = new SettingsViewModel
        {
            Hosts = await _settings.GetHostsAsync(),
            Areas = await _settings.GetAreasAsync(),
            Purposes = await _settings.GetPurposesAsync()
        };
        return View(model);
    }

    // ---- Qəbul edən şəxslər ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddHost(string firstName, string lastName, string? email, string? phone, string? department) =>
        Guard(() => _settings.AddHostAsync(new HostInputDto
        { FirstName = firstName, LastName = lastName, Email = email, Phone = phone, Department = department }),
        "Qəbul edən əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> UpdateHost(long id, string firstName, string lastName, string? email, string? phone, string? department) =>
        Guard(() => _settings.UpdateHostAsync(id, new HostInputDto
        { FirstName = firstName, LastName = lastName, Email = email, Phone = phone, Department = department }),
        "Qəbul edən yeniləndi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleHost(long id) =>
        Guard(() => _settings.ToggleHostAsync(id), "Status dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteHost(long id) =>
        Guard(() => _settings.DeleteHostAsync(id), "Qəbul edən silindi.");

    // ---- Ərazilər ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddArea(string name) =>
        Guard(() => _settings.AddAreaAsync(name), "Ərazi əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteArea(long id) =>
        Guard(() => _settings.DeleteAreaAsync(id), "Ərazi silindi.");

    // ---- Məqsədlər ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddPurpose(string name) =>
        Guard(() => _settings.AddPurposeAsync(name), "Məqsəd əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> TogglePurpose(long id) =>
        Guard(() => _settings.TogglePurposeAsync(id), "Məqsədin statusu dəyişdirildi.");

    private async Task<IActionResult> Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
