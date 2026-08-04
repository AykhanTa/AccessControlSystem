using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using AccessControlSystem.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class EmployeesController : Controller
{
    private readonly IEmployeeService _employees;
    private readonly ISettingsService _settings;
    private readonly ILookupService _lookups;
    private readonly IWebHostEnvironment _env;
    private readonly EmployeeSyncService _sync;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    public EmployeesController(IEmployeeService employees, ISettingsService settings,
        ILookupService lookups, IWebHostEnvironment env, EmployeeSyncService sync)
    {
        _employees = employees;
        _settings = settings;
        _lookups = lookups;
        _env = env;
        _sync = sync;
    }

    public async Task<IActionResult> Index()
    {
        var model = new EmployeesViewModel
        {
            Employees = await _employees.GetAllAsync(),
            Companies = await _settings.GetCompaniesAsync(),
            Departments = await _settings.GetDepartmentsAsync(),
            Positions = await _settings.GetPositionsAsync(),
            Floors = await _lookups.GetFloorsAsync(),
            Devices = await _settings.GetDevicesAsync()
        };
        return View(model);
    }

    /// <summary>Canlı UI üçün — işçi mövqeləri (JSON, polling).</summary>
    [HttpGet]
    public async Task<IActionResult> StatusFeed() => Json(await _employees.GetPresencesAsync());

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Add)]
    public async Task<IActionResult> Create(EmployeeFormModel form)
    {
        try
        {
            var dto = ToDto(form);
            dto.PhotoPath = await SaveFileAsync(form.Photo);
            await _employees.CreateAsync(dto);
            TempData["Success"] = $"{form.LastName} {form.FirstName} işçisi əlavə edildi.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Edit)]
    public async Task<IActionResult> Update(EmployeeFormModel form)
    {
        try
        {
            var dto = ToDto(form);
            dto.PhotoPath = await SaveFileAsync(form.Photo);
            await _employees.UpdateAsync(form.Id, dto);
            TempData["Success"] = "İşçi məlumatları yeniləndi.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or KeyNotFoundException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Edit)]
    public async Task<IActionResult> Sync(long id)
    {
        try { TempData["Success"] = "Cihaza sinxronizasiya: " + await _sync.SyncAsync(id); }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Edit)]
    public async Task<IActionResult> ToggleStatus(long id)
    {
        try { await _employees.ToggleStatusAsync(id); TempData["Success"] = "Status dəyişdirildi."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        try { await _employees.DeleteAsync(id); TempData["Success"] = "İşçi silindi."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private static EmployeeCreateDto ToDto(EmployeeFormModel f) => new()
    {
        FirstName = f.FirstName,
        LastName = f.LastName,
        Patronymic = f.Patronymic,
        EmployeeNo = f.EmployeeNo,
        FinCode = f.FinCode,
        DocumentNo = f.DocumentNo,
        Phone = f.Phone,
        Email = f.Email,
        CompanyId = f.CompanyId,
        DepartmentId = f.DepartmentId,
        PositionId = f.PositionId,
        EmploymentStartAt = f.EmploymentStartAt,
        DeviceNumbers = f.DeviceNumbers,
        DeviceName = f.DeviceName,
        FloorIds = f.FloorIds ?? new()
    };

    private async Task<string?> SaveFileAsync(IFormFile? file)
    {
        if (file is null || file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Yalnız şəkil (jpg, png, webp) yükləmək olar.");
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", "employees");
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(stream);
        return $"/uploads/employees/{name}";
    }
}
