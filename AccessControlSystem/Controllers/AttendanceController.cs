using AccessControlSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>Davamiyyət — iş cədvəlinə əsasən gündəlik və aylıq nəticələr.
/// İcazə "reports" bölməsindən (SectionMap: Attendance → reports).</summary>
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendance;
    private readonly ISettingsService _settings;
    private readonly IEmployeeService _employees;

    public AttendanceController(IAttendanceService attendance, ISettingsService settings, IEmployeeService employees)
    {
        _attendance = attendance;
        _settings = settings;
        _employees = employees;
    }

    public async Task<IActionResult> Index()
    {
        ViewData["Active"] = "attendance";
        ViewData["Heading"] = "Davamiyyət";
        ViewData["Title"] = "Davamiyyət";
        ViewBag.Companies = await _settings.GetCompaniesAsync();
        ViewBag.Departments = await _settings.GetDepartmentsAsync();
        ViewBag.Employees = await _employees.GetAllAsync();
        ViewBag.Today = DateTime.Today.ToString("yyyy-MM-dd");
        ViewBag.Year = DateTime.Today.Year;
        ViewBag.Month = DateTime.Today.Month;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> DailyBody(long? companyId, long? deptId, long? employeeId,
        DateTime? from, DateTime? to, string? kind)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) t = f;
        return PartialView("_DailyBody", await _attendance.GetDailyAsync(companyId, deptId, employeeId, f, t, kind));
    }

    /// <summary>Verilmiş günü əhatə edən həftə (B.e–Bazar) üçün gündəlik cədvəl.</summary>
    [HttpGet]
    public async Task<IActionResult> WeeklyBody(long? companyId, long? deptId, long? employeeId, DateTime? date, string? kind)
    {
        var (monday, sunday) = WeekOf(date ?? DateTime.Today);
        return PartialView("_DailyBody", await _attendance.GetDailyAsync(companyId, deptId, employeeId, monday, sunday, kind));
    }

    /// <summary>İşçi üzrə yekun (aralıq boyu bir sətir).</summary>
    [HttpGet]
    public async Task<IActionResult> SummaryBody(long? companyId, long? deptId, long? employeeId,
        DateTime? from, DateTime? to, string? kind)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) t = f;
        return PartialView("_SummaryBody", await _attendance.GetSummaryAsync(companyId, deptId, employeeId, f, t, kind));
    }

    /// <summary>İşçi üzrə yekunu Excel (.xls) kimi endirir.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportSummary(long? companyId, long? deptId, long? employeeId,
        DateTime? from, DateTime? to, string? kind)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) t = f;
        var data = await _attendance.GetSummaryAsync(companyId, deptId, employeeId, f, t, kind);

        var sb = new System.Text.StringBuilder();
        sb.Append("<html><head><meta charset=\"utf-8\"></head><body>");
        sb.Append($"<h3>Davamiyyət — İşçi üzrə yekun ({Esc(data.KindLabel)})</h3>");
        sb.Append($"<p>{Esc(data.Scope)} · {data.FromLabel}–{data.ToLabel}</p>");
        sb.Append("<table border=\"1\" cellspacing=\"0\" cellpadding=\"4\"><tr>");
        foreach (var h in new[] { "İşçi İD", "İşçi", "Şöbə", "İş günü", "Gəlmədi", "Gecikmə (gün)", "Gecikmə (dəq)",
                                  "Erkən (gün)", "Əlavə iş (dəq)", "İşlənmiş (dəq)", "Məzuniyyət", "Ezamiyyət", "Bayram" })
            sb.Append($"<th>{Esc(h)}</th>");
        sb.Append("</tr>");
        foreach (var r in data.Rows)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{Esc(r.EmployeeNo)}</td><td>{Esc(r.FullName)}</td><td>{Esc(r.Department ?? "")}</td>");
            sb.Append($"<td>{r.PresentDays}</td><td>{r.AbsentDays}</td><td>{r.LateDays}</td><td>{r.LateMin}</td>");
            sb.Append($"<td>{r.EarlyDays}</td><td>{r.OvertimeMin}</td><td>{r.WorkedMin}</td><td>{r.LeaveDays}</td><td>{r.TripDays}</td><td>{r.HolidayDays}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</table></body></html>");
        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "application/vnd.ms-excel", $"davamiyyet_yekun_{f:yyyyMMdd}_{t:yyyyMMdd}.xls");
    }

    /// <summary>Gündəlik/həftəlik hesabatı Excel (.xls) faylı kimi endirir.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportDaily(long? companyId, long? deptId, long? employeeId,
        DateTime? from, DateTime? to, string? kind)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) t = f;
        var data = await _attendance.GetDailyAsync(companyId, deptId, employeeId, f, t, kind);
        var k = data.Kind;
        // Növə uyğun sütunlar (ekrandakı ilə eyni məntiq — artıq sütun olmasın).
        bool showIn = k is "" or "late" or "incomplete";
        bool showOut = k is "" or "early" or "incomplete" or "overtime";
        bool showLate = k is "" or "late";
        bool showEarly = k is "" or "early";
        bool showOver = k is "" or "overtime";
        bool showWorked = k is "" or "overtime";

        var sb = new System.Text.StringBuilder();
        sb.Append("<html><head><meta charset=\"utf-8\"></head><body>");
        sb.Append($"<h3>Davamiyyət — {Esc(data.KindLabel)}</h3>");
        sb.Append($"<p>{Esc(data.Scope)} · {data.FromLabel}–{data.ToLabel}</p>");
        sb.Append("<table border=\"1\" cellspacing=\"0\" cellpadding=\"4\"><tr>");
        void Th(string h) => sb.Append($"<th>{Esc(h)}</th>");
        Th("Tarix"); Th("Gün"); Th("İşçi İD"); Th("İşçi"); Th("Şöbə"); Th("Cədvəl");
        if (showIn) Th("Giriş");
        if (showOut) Th("Çıxış");
        if (showLate) Th("Gecikmə (dəq)");
        if (showEarly) Th("Erkən (dəq)");
        if (showOver) Th("Əlavə iş (dəq)");
        if (showWorked) Th("İşlənmiş (dəq)");
        Th("Status");
        sb.Append("</tr>");
        foreach (var r in data.Rows)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{Esc(r.Date)}</td><td>{Esc(r.Weekday)}</td><td>{Esc(r.EmployeeNo)}</td>");
            sb.Append($"<td>{Esc(r.FullName)}</td><td>{Esc(r.Department ?? "")}</td><td>{Esc(r.Schedule)}</td>");
            if (showIn) sb.Append($"<td>{Esc(r.In ?? "")}</td>");
            if (showOut) sb.Append($"<td>{Esc(r.Out ?? "")}</td>");
            if (showLate) sb.Append($"<td>{r.LateMin}</td>");
            if (showEarly) sb.Append($"<td>{r.EarlyMin}</td>");
            if (showOver) sb.Append($"<td>{r.OvertimeMin}</td>");
            if (showWorked) sb.Append($"<td>{r.WorkedMin}</td>");
            sb.Append($"<td>{Esc(r.StatusLabel)}</td></tr>");
        }
        sb.Append("</table></body></html>");

        var bytes = System.Text.Encoding.UTF8.GetPreamble()
            .Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "application/vnd.ms-excel", $"davamiyyet_{f:yyyyMMdd}_{t:yyyyMMdd}.xls");
    }

    private static string Esc(string s) =>
        System.Net.WebUtility.HtmlEncode(s ?? "");

    private static (DateTime monday, DateTime sunday) WeekOf(DateTime d)
    {
        int diff = ((int)d.Date.DayOfWeek + 6) % 7;   // Bazar=0 → B.e-yə qədər geriyə
        var monday = d.Date.AddDays(-diff);
        return (monday, monday.AddDays(6));
    }

    [HttpGet]
    public async Task<IActionResult> MonthlyBody(long? companyId, long? deptId, long? employeeId, int? year, int? month)
    {
        var y = year ?? DateTime.Today.Year;
        var m = month ?? DateTime.Today.Month;
        return PartialView("_MonthlyBody", await _attendance.GetMonthlyAsync(companyId, deptId, employeeId, y, m));
    }
}
