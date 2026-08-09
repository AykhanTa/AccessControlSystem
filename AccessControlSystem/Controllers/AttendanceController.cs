using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>İştirak — iş cədvəlinə əsasən gündəlik və aylıq nəticələr.
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
        ViewData["Heading"] = "İştirak";
        ViewData["Title"] = "İştirak";
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

    /// <summary>İşçi üzrə yekunu real Excel (.xlsx) faylı kimi endirir. Gizlədilmiş sütunlar (hide) buraxılmır.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportSummary(long? companyId, long? deptId, long? employeeId,
        DateTime? from, DateTime? to, string? kind, string? hide)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) t = f;
        var data = await _attendance.GetSummaryAsync(companyId, deptId, employeeId, f, t, kind);
        var hidden = ParseHide(hide);

        // (başlıq, açar, dəyər) — açar view-dakı data-col ilə eynidir.
        var cols = new List<(string H, string Key, Func<AttSumRowDto, XLCellValue> V)>
        {
            ("İşçi İD", "emp", r => r.EmployeeNo),
            ("İşçi", "emp", r => r.FullName),
            ("Şöbə", "dept", r => r.Department ?? ""),
            ("İş günü", "present", r => r.PresentDays),
            ("Gəlmədi", "absent", r => r.AbsentDays),
            ("Gecikmə (gün)", "lateDays", r => r.LateDays),
            ("Gecikmə (dəq)", "lateMin", r => r.LateMin),
            ("Erkən (gün)", "earlyDays", r => r.EarlyDays),
            ("Əlavə iş (dəq)", "over", r => r.OvertimeMin),
            ("İşlənmiş (dəq)", "worked", r => r.WorkedMin),
            ("Məzuniyyət", "leave", r => r.LeaveDays),
            ("Ezamiyyət", "trip", r => r.TripDays),
            ("Bayram", "holiday", r => r.HolidayDays),
        }.Where(c => !hidden.Contains(c.Key)).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Yekun");
        WriteTitle(ws, $"İştirak — İşçi üzrə yekun ({data.KindLabel})", $"{data.Scope} · {data.FromLabel}–{data.ToLabel}");

        const int hr = 4;
        for (var i = 0; i < cols.Count; i++) StyleHeader(ws.Cell(hr, i + 1), cols[i].H);
        var rr = hr + 1;
        foreach (var r in data.Rows)
        {
            for (var i = 0; i < cols.Count; i++) ws.Cell(rr, i + 1).Value = cols[i].V(r);
            rr++;
        }
        Finalize(ws, hr, cols.Count);
        return Xlsx(wb, $"davamiyyet_yekun_{f:yyyyMMdd}_{t:yyyyMMdd}.xlsx");
    }

    /// <summary>Gündəlik/həftəlik hesabatı real Excel (.xlsx) faylı kimi endirir. Gizlədilmiş sütunlar (hide) buraxılmır.</summary>
    [HttpGet]
    public async Task<IActionResult> ExportDaily(long? companyId, long? deptId, long? employeeId,
        DateTime? from, DateTime? to, string? kind, string? hide)
    {
        var f = (from ?? DateTime.Today).Date;
        var t = (to ?? DateTime.Today).Date;
        if (t < f) t = f;
        var data = await _attendance.GetDailyAsync(companyId, deptId, employeeId, f, t, kind);
        var k = data.Kind;
        var hidden = ParseHide(hide);
        // Növə uyğun sütunlar (ekrandakı ilə eyni məntiq).
        bool showIn = k is "" or "late" or "incomplete";
        bool showOut = k is "" or "early" or "incomplete" or "overtime";
        bool showLate = k is "" or "late";
        bool showEarly = k is "" or "early";
        bool showOver = k is "" or "overtime";
        bool showWorked = k is "" or "overtime";

        var cols = new List<(string H, string Key, Func<AttDayRowDto, XLCellValue> V)>
        {
            ("Tarix", "date", r => r.Date),
            ("Gün", "weekday", r => r.Weekday),
            ("İşçi İD", "emp", r => r.EmployeeNo),
            ("İşçi", "emp", r => r.FullName),
            ("Şöbə", "dept", r => r.Department ?? ""),
            ("Cədvəl", "sched", r => r.Schedule),
        };
        if (showIn) cols.Add(("Giriş", "in", r => r.In ?? ""));
        if (showOut) cols.Add(("Çıxış", "out", r => r.Out ?? ""));
        if (showLate) cols.Add(("Gecikmə (dəq)", "late", r => r.LateMin));
        if (showEarly) cols.Add(("Erkən (dəq)", "early", r => r.EarlyMin));
        if (showOver) cols.Add(("Əlavə iş (dəq)", "over", r => r.OvertimeMin));
        if (showWorked) cols.Add(("İşlənmiş (dəq)", "worked", r => r.WorkedMin));
        cols.Add(("Status", "status", r => r.StatusLabel));
        cols = cols.Where(c => !hidden.Contains(c.Key)).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("İştirak");
        WriteTitle(ws, $"İştirak — {data.KindLabel}", $"{data.Scope} · {data.FromLabel}–{data.ToLabel}");

        const int hr = 4;
        for (var i = 0; i < cols.Count; i++) StyleHeader(ws.Cell(hr, i + 1), cols[i].H);
        var rr = hr + 1;
        foreach (var r in data.Rows)
        {
            for (var i = 0; i < cols.Count; i++) ws.Cell(rr, i + 1).Value = cols[i].V(r);
            rr++;
        }
        Finalize(ws, hr, cols.Count);
        return Xlsx(wb, $"davamiyyet_{f:yyyyMMdd}_{t:yyyyMMdd}.xlsx");
    }

    private static HashSet<string> ParseHide(string? hide) =>
        (hide ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();

    // ---- Excel köməkçiləri (ClosedXML) ----
    private static void WriteTitle(IXLWorksheet ws, string title, string sub)
    {
        var c1 = ws.Cell(1, 1); c1.Value = title; c1.Style.Font.Bold = true; c1.Style.Font.FontSize = 14;
        var c2 = ws.Cell(2, 1); c2.Value = sub; c2.Style.Font.FontColor = XLColor.Gray;
    }
    private static void StyleHeader(IXLCell cell, string text)
    {
        cell.Value = text;
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EEF2F7");
        cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
    }
    private static void Finalize(IXLWorksheet ws, int headerRow, int lastCol)
    {
        if (lastCol < 1) return;
        ws.Range(headerRow, 1, headerRow, lastCol).SetAutoFilter();
        ws.SheetView.FreezeRows(headerRow);
        ws.Columns().AdjustToContents();
    }
    private IActionResult Xlsx(XLWorkbook wb, string fileName)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return File(ms.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

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
