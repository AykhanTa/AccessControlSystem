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
            Purposes = await _settings.GetPurposesAsync(),
            Floors = await _settings.GetFloorsAsync(),
            Devices = await _settings.GetDevicesAsync()
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

    // ---- Mərtəbələr ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddFloor(string name) =>
        Guard(() => _settings.AddFloorAsync(name), "Mərtəbə əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleFloor(long id) =>
        Guard(() => _settings.ToggleFloorAsync(id), "Mərtəbənin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteFloor(long id) =>
        Guard(() => _settings.DeleteFloorAsync(id), "Mərtəbə silindi.");

    // ---- Cihazlar ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddDevice(string name, string ip, int port, bool useHttps, int doorNo, long floorId, string direction) =>
        Guard(() => _settings.AddDeviceAsync(new DeviceInputDto
        { Name = name, Ip = ip, Port = port, UseHttps = useHttps, DoorNo = doorNo, FloorId = floorId, Direction = direction }),
        "Cihaz əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> UpdateDevice(long id, string name, string ip, int port, bool useHttps, int doorNo, long floorId, string direction) =>
        Guard(() => _settings.UpdateDeviceAsync(id, new DeviceInputDto
        { Name = name, Ip = ip, Port = port, UseHttps = useHttps, DoorNo = doorNo, FloorId = floorId, Direction = direction }),
        "Cihaz yeniləndi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleDevice(long id) =>
        Guard(() => _settings.ToggleDeviceAsync(id), "Cihazın statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteDevice(long id) =>
        Guard(() => _settings.DeleteDeviceAsync(id), "Cihaz silindi.");

    private async Task<IActionResult> Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
