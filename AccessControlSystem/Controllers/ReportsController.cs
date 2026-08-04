using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reports;
    private readonly ISettingsService _settings;

    public ReportsController(IReportService reports, ISettingsService settings)
    {
        _reports = reports;
        _settings = settings;
    }

    /// <summary>İllik hesabat — il seçilə bilər (default: ən son il / cari il).</summary>
    public async Task<IActionResult> Index(int? year)
    {
        var years = await _reports.GetYearsAsync();
        var selectedYear = year ?? (years.Count > 0 ? years[0] : DateTime.Today.Year);

        var model = new ReportsViewModel
        {
            Years = years,
            SelectedYear = selectedYear,
            Report = await _reports.GetReportAsync(selectedYear)
        };
        return View(model);
    }

    /// <summary>Şöbə üzrə hesabat — səhifə qabığı (yalnız form). Ağır sorğu AJAX ilə gəlir → ani açılır.</summary>
    public async Task<IActionResult> DepartmentAccess(long? companyId, long? deptId, string? from, string? to)
    {
        ViewBag.Companies = await _settings.GetCompaniesAsync();
        ViewBag.Departments = await _settings.GetDepartmentsAsync();
        ViewBag.CompanyId = companyId;
        ViewBag.DeptId = deptId;
        ViewBag.From = string.IsNullOrWhiteSpace(from) ? DateTime.Today.ToString("yyyy-MM-dd") : from;
        ViewBag.To = string.IsNullOrWhiteSpace(to) ? DateTime.Today.ToString("yyyy-MM-dd") : to;
        return View();
    }

    /// <summary>Hesabatın gövdəsi (AJAX partial) — cihaz(lar)dan data çəkir.</summary>
    [HttpGet]
    public async Task<IActionResult> DepartmentAccessBody(long? companyId, long? deptId, DateTime? from, DateTime? to)
    {
        var fStart = (from ?? DateTime.Today).Date;
        var tEnd = (to ?? DateTime.Today).Date.AddDays(1).AddSeconds(-1);
        ViewBag.From = fStart.ToString("yyyy-MM-dd");
        ViewBag.To = tEnd.ToString("yyyy-MM-dd");
        return PartialView("_DepartmentAccessBody", await _reports.GetDepartmentAccessAsync(companyId, deptId, fStart, tEnd));
    }
}
