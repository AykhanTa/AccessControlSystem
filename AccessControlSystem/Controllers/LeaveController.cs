using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>HR: məzuniyyət/ezamiyyət növləri, işçi qeydləri və bayram təqvimi.
/// Səhifə Parametrlərin "Məzuniyyət &amp; Bayram" tabındadır — bu kontroller yalnız POST-ları emal edir.
/// İcazə "employees" bölməsindən (SectionMap: Leave → employees).</summary>
public class LeaveController : Controller
{
    private const string Tab = "leave";

    private readonly ILeaveService _leave;

    public LeaveController(ILeaveService leave) => _leave = leave;

    /// <summary>Köhnə /Leave ünvanı — Parametrlərin məzuniyyət tabına yönləndirilir.</summary>
    public IActionResult Index(int? year) =>
        RedirectToAction("Index", "Settings", new { tab = Tab, year });

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
        return RedirectToAction("Index", "Settings", new { tab = Tab });
    }
}
