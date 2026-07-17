using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class ReportsController : Controller
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

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
}
