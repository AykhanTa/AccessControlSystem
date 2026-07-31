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
            Companies = await _settings.GetCompaniesAsync(),
            Departments = await _settings.GetDepartmentsAsync(),
            Positions = await _settings.GetPositionsAsync(),
            Centers = await _settings.GetCentersAsync(),
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

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeletePurpose(long id) =>
        Guard(() => _settings.DeletePurposeAsync(id), "Məqsəd silindi.");

    // ---- Şirkətlər ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddCompany(string name, string? taxNumber, string? contactPerson, string? phone, string? email) =>
        Guard(() => _settings.AddCompanyAsync(new CompanyInputDto
        { Name = name, TaxNumber = taxNumber, ContactPerson = contactPerson, Phone = phone, Email = email }),
        "Şirkət əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleCompany(long id) =>
        Guard(() => _settings.ToggleCompanyAsync(id), "Şirkətin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteCompany(long id) =>
        Guard(() => _settings.DeleteCompanyAsync(id), "Şirkət silindi.");

    // ---- Şöbələr ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddDepartment(string name, long companyId, long? parentId) =>
        Guard(() => _settings.AddDepartmentAsync(name, companyId, parentId), "Şöbə əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleDepartment(long id) =>
        Guard(() => _settings.ToggleDepartmentAsync(id), "Şöbənin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteDepartment(long id) =>
        Guard(() => _settings.DeleteDepartmentAsync(id), "Şöbə silindi.");

    // ---- Vəzifələr ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddPosition(string name, long companyId) =>
        Guard(() => _settings.AddPositionAsync(name, companyId), "Vəzifə əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> TogglePosition(long id) =>
        Guard(() => _settings.TogglePositionAsync(id), "Vəzifənin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeletePosition(long id) =>
        Guard(() => _settings.DeletePositionAsync(id), "Vəzifə silindi.");

    // ---- Mərkəzlər ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddCenter(string code, string name, string? address, string? city) =>
        Guard(() => _settings.AddCenterAsync(new CenterInputDto { Code = code, Name = name, Address = address, City = city }),
        "Mərkəz əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> UpdateCenter(long id, string code, string name, string? address, string? city) =>
        Guard(() => _settings.UpdateCenterAsync(id, new CenterInputDto { Code = code, Name = name, Address = address, City = city }),
        "Mərkəz yeniləndi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleCenter(long id) =>
        Guard(() => _settings.ToggleCenterAsync(id), "Mərkəzin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteCenter(long id) =>
        Guard(() => _settings.DeleteCenterAsync(id), "Mərkəz silindi.");

    // ---- Mərtəbələr ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddFloor(string name, long? centerId) =>
        Guard(() => _settings.AddFloorAsync(name, centerId), "Mərtəbə əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> ToggleFloor(long id) =>
        Guard(() => _settings.ToggleFloorAsync(id), "Mərtəbənin statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Delete)]
    public Task<IActionResult> DeleteFloor(long id) =>
        Guard(() => _settings.DeleteFloorAsync(id), "Mərtəbə silindi.");

    // ---- Cihazlar ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Add)]
    public Task<IActionResult> AddDevice(string name, string ip, int port, bool useHttps, int doorNo, long floorId, string direction, string pointType) =>
        Guard(() => _settings.AddDeviceAsync(new DeviceInputDto
        { Name = name, Ip = ip, Port = port, UseHttps = useHttps, DoorNo = doorNo, FloorId = floorId, Direction = direction, PointType = pointType }),
        "Cihaz əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("settings", PermType.Edit)]
    public Task<IActionResult> UpdateDevice(long id, string name, string ip, int port, bool useHttps, int doorNo, long floorId, string direction, string pointType) =>
        Guard(() => _settings.UpdateDeviceAsync(id, new DeviceInputDto
        { Name = name, Ip = ip, Port = port, UseHttps = useHttps, DoorNo = doorNo, FloorId = floorId, Direction = direction, PointType = pointType }),
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
