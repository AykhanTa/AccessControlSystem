using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>HR: məzuniyyət/ezamiyyət növləri, işçi qeydləri və bayram təqvimi.
/// İcazə "employees" bölməsindən (SectionMap: Leave → employees).</summary>
public class LeaveController : Controller
{
    private readonly ILeaveService _leave;
    private readonly ISettingsService _settings;
    private readonly IEmployeeService _employees;

    public LeaveController(ILeaveService leave, ISettingsService settings, IEmployeeService employees)
    {
        _leave = leave;
        _settings = settings;
        _employees = employees;
    }

    public async Task<IActionResult> Index(int? year)
    {
        var y = year ?? DateTime.Today.Year;
        ViewData["Active"] = "leave";
        ViewData["Heading"] = "Məzuniyyət & Bayram";
        ViewData["Title"] = "Məzuniyyət & Bayram";
        var model = new LeaveViewModel
        {
            Year = y,
            Types = await _leave.GetTypesAsync(),
            TypeLookup = await _leave.GetTypeLookupAsync(),
            Holidays = await _leave.GetHolidaysAsync(y),
            Records = await _leave.GetRecordsAsync(new DateTime(y, 1, 1), new DateTime(y, 12, 31), null, null),
            Employees = await _employees.GetAllAsync(),
            Companies = await _settings.GetCompaniesAsync()
        };
        return View(model);
    }

    // ---- Növlər ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Add)]
    public Task<IActionResult> AddType(string name, bool countsAsWorked, string? color, long? companyId) =>
        Guard(() => _leave.AddTypeAsync(new LeaveTypeInputDto
        { Name = name, CountsAsWorked = countsAsWorked, Paid = true, Color = color, CompanyId = companyId }),
        "Növ əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Edit)]
    public Task<IActionResult> ToggleType(long id) =>
        Guard(() => _leave.ToggleTypeAsync(id), "Növün statusu dəyişdirildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Delete)]
    public Task<IActionResult> DeleteType(long id) =>
        Guard(() => _leave.DeleteTypeAsync(id), "Növ silindi.");

    // ---- İşçi qeydləri ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Add)]
    public Task<IActionResult> AddRecord(long employeeId, long leaveTypeId, string startDate, string endDate, string? reason) =>
        Guard(() => _leave.AddRecordAsync(new LeaveRecordInputDto
        { EmployeeId = employeeId, LeaveTypeId = leaveTypeId, StartDate = startDate, EndDate = endDate, Reason = reason }),
        "Qeyd əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Delete)]
    public Task<IActionResult> DeleteRecord(long id) =>
        Guard(() => _leave.DeleteRecordAsync(id), "Qeyd silindi.");

    // ---- Bayramlar ----
    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Add)]
    public Task<IActionResult> AddHoliday(string startDate, string endDate, string name, long? companyId) =>
        Guard(() => _leave.AddHolidayAsync(new HolidayInputDto
        { StartDate = startDate, EndDate = endDate, Name = name, CompanyId = companyId }),
        "Bayram əlavə edildi.");

    [HttpPost, ValidateAntiForgeryToken, RequirePermission("employees", PermType.Delete)]
    public Task<IActionResult> DeleteHoliday(string ids)
    {
        var list = (ids ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => long.TryParse(s, out var v) ? v : 0).Where(v => v > 0);
        return Guard(() => _leave.DeleteHolidaysAsync(list), "Bayram silindi.");
    }

    private async Task<IActionResult> Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
