using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly IVisitRepository _visits;
    private readonly IEmployeeRepository _employees;
    private readonly IDeviceRepository _devices;
    private readonly IHikvisionDeviceService _hik;
    private readonly HikvisionOptions _opt;

    private static readonly string[] MonthNames =
        { "yanvar", "fevral", "mart", "aprel", "may", "iyun",
          "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };

    public ReportService(IVisitRepository visits, IEmployeeRepository employees,
        IDeviceRepository devices, IHikvisionDeviceService hik, HikvisionOptions opt)
    {
        _visits = visits;
        _employees = employees;
        _devices = devices;
        _hik = hik;
        _opt = opt;
    }

    public async Task<DeptAccessReportDto> GetDepartmentAccessAsync(long? companyId, long? departmentId,
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        // BÜTÜN işçilər — uyğunlaşma üçün (hadisə hansı işçiyə aiddir); göstərilən dəst sonra süzülür.
        var allEmployees = await _employees.GetAllAsync(ct);
        var matcher = new EmployeeMatcher(allEmployees);

        // Keçidləri BİRBAŞA cihaz(lar)ın jurnalından xam çək (cihaz, nömrə, ad, vaxt, istiqamət).
        var raws = await FetchRawScansAsync(new DateTimeOffset(from), new DateTimeOffset(to), ct);

        var scansByEmp = new Dictionary<long, List<Scan>>();
        foreach (var r in raws)
        {
            var empId = matcher.Resolve(r.deviceId, r.number, r.name);
            if (empId is null) continue;
            if (!scansByEmp.TryGetValue(empId.Value, out var l)) scansByEmp[empId.Value] = l = new();
            l.Add(new Scan(r.time, r.isExit));
        }

        var employees = allEmployees;
        if (companyId is { } cmp)
            employees = employees.Where(e => e.CompanyId == cmp).ToList();
        if (departmentId is { } did)
            employees = employees.Where(e => e.DepartmentId == did).ToList();

        List<Scan> ScansFor(Employee e) => scansByEmp.TryGetValue(e.Id, out var s) ? s : new();

        var groups = employees
            .GroupBy(e => new
            {
                Dept = e.Department?.Name ?? "Şöbə təyin edilməyib",
                Company = e.Company?.Name
            })
            .OrderBy(g => g.Key.Dept, StringComparer.CurrentCulture)
            .Select(g => new DeptGroupDto
            {
                Department = g.Key.Dept,
                Company = g.Key.Company,
                Employees = g
                    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                    .Select(e => BuildRow(e, ScansFor(e)))
                    .ToList()
            })
            .ToList();

        return new DeptAccessReportDto
        {
            FromLabel = from.ToString("dd.MM.yyyy HH:mm"),
            ToLabel = to.ToString("dd.MM.yyyy HH:mm"),
            Scope = BuildScope(companyId, departmentId, employees),
            TotalEmployees = employees.Count,
            TotalEvents = groups.Sum(g => g.Employees.Sum(e => e.TotalEvents)),
            Groups = groups
        };
    }

    /// <summary>Hesabatın əhatə etiketini qurur (müəssisə · şöbə).</summary>
    private static string BuildScope(long? companyId, long? departmentId, List<Employee> employees)
    {
        var company = companyId is null ? "Bütün müəssisələr"
            : employees.FirstOrDefault()?.Company?.Name ?? "Müəssisə";
        var dept = departmentId is null ? "bütün şöbələr"
            : employees.FirstOrDefault()?.Department?.Name ?? "şöbə";
        return $"{company} · {dept}";
    }

    private readonly record struct Scan(DateTime Time, bool IsExit);

    private readonly record struct RawScan(long deviceId, string number, string? name, DateTime time, bool isExit);

    /// <summary>Bütün aktiv cihazların jurnalını aralıq üçün XAM çəkir (uyğunlaşma sonra edilir).</summary>
    private async Task<List<RawScan>> FetchRawScansAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var devices = (await _devices.GetAllWithFloorAsync(ct)).Where(x => x.IsActive).ToList();
        // Hər cihazı PARALEL sorğula (sıra ilə deyil) — çoxlu cihazda gözləmə vaxtını kəskin azaldır.
        var perDevice = await Task.WhenAll(devices.Select(d => FetchOneDeviceRawAsync(d, from, to, ct)));
        return perDevice.SelectMany(x => x).ToList();
    }

    private async Task<List<RawScan>> FetchOneDeviceRawAsync(
        Device d, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var result = new List<RawScan>();
        var target = new HikDevice(d.Ip, _opt.Username, _opt.Password,
            d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);
        var isExit = (d.AccessPoint?.Direction ?? d.Direction) == DeviceDirection.Exit;
        var position = 0;

        for (var page = 0; page < 200; page++)   // təhlükəsizlik limiti
        {
            HikEventRawPage res;
            try { res = await _hik.SearchEventPageAsync(target, from, to, position, 100, ct: ct); }
            catch { break; }
            if (!res.Ok || res.Items.Count == 0) break;

            foreach (var e in res.Items)
            {
                if (string.IsNullOrWhiteSpace(e.EmployeeNo)) continue;   // yalnız şəxsli auth (qapı/login yox)
                if (e.Minor is not (1 or 75)) continue;                 // kart/QR = 1, üz = 75
                if (e.Time is not { } t) continue;
                result.Add(new RawScan(d.Id, e.EmployeeNo!.Trim(), e.Name, t.LocalDateTime, isExit));
            }

            position += res.Items.Count;
            if (res.Status != "MORE") break;
        }
        return result;
    }

    private static DeptEmployeeRowDto BuildRow(Employee e, List<Scan> scans)
    {
        var entries = scans.Where(s => !s.IsExit).Select(s => s.Time).ToList();
        var exits = scans.Where(s => s.IsExit).Select(s => s.Time).ToList();
        var all = scans.Select(s => s.Time).ToList();

        return new DeptEmployeeRowDto
        {
            EmployeeNo = e.EmployeeNo,
            FullName = e.FullName,
            Position = e.Position?.Name,
            EntryCount = entries.Count,
            ExitCount = exits.Count,
            // İlk giriş = ilk keçid (giriş cihazı varsa ondan), Son çıxış = son keçid (çıxış cihazı varsa ondan).
            FirstIn = all.Count == 0 ? null
                      : (entries.Count > 0 ? entries.Min() : all.Min()).ToString("dd.MM.yyyy HH:mm:ss"),
            LastOut = all.Count == 0 ? null
                      : (exits.Count > 0 ? exits.Max() : all.Max()).ToString("dd.MM.yyyy HH:mm:ss"),
            TotalEvents = all.Count,
            Events = new()
        };
    }

    public Task<List<int>> GetYearsAsync(CancellationToken ct = default) =>
        _visits.GetDistinctYearsAsync(ct);

    public async Task<ReportDto> GetReportAsync(int year, CancellationToken ct = default)
    {
        var visits = await _visits.GetForReportAsync(year, ct);

        var exits = visits.Count(v => v.Status == VisitStatus.Out);
        var entries = visits.Count;   // hər ziyarətin gəlişi var

        // Orta qalma müddəti — yalnız müsbət müddətli, çıxışı təsdiqlənmiş ziyarətlər
        var durations = visits
            .Where(v => v.Status == VisitStatus.Out && v.ActualExitAt != null)
            .Select(v => (v.ActualExitAt!.Value - v.ArrivalAt).TotalMinutes)
            .Where(d => d > 0)
            .ToList();

        // Aylıq jurnal (12 ay, sıfırlar da göstərilir)
        var monthCounts = new int[12];
        foreach (var v in visits) monthCounts[v.ArrivalAt.Month - 1]++;
        var months = MonthNames
            .Select((name, i) => new LabelCount { Label = name, Count = monthCounts[i] })
            .ToList();

        return new ReportDto
        {
            Year = year,
            TotalVisits = visits.Count,
            UniqueGuests = visits.Select(v => v.GuestId).Distinct().Count(),
            Entries = entries,
            Exits = exits,
            CardUse = visits.Count(v => v.PassType == PassType.Card),
            QrUse = visits.Count(v => v.PassType == PassType.Qr),
            Inside = visits.Count(v => v.Status is VisitStatus.In or VisitStatus.Late),
            Late = visits.Count(v => v.Status == VisitStatus.Late),
            ExitRatePercent = entries > 0 ? (int)Math.Round(exits * 100.0 / entries) : 0,
            AvgStayMinutes = durations.Count > 0 ? (int)Math.Round(durations.Average()) : 0,
            Months = months,
            Purposes = Tally(visits.SelectMany(v => v.VisitPurposes.Select(p => p.Purpose.Name))),
            Hosts = Tally(visits.Select(v => v.Host.FullName)),
            Areas = Tally(visits.SelectMany(v => v.VisitAreas.Select(a => a.Area.Name))),
            Cards = Tally(visits.Where(v => v.PassType == PassType.Card && v.Card != null)
                                 .Select(v => v.Card!.CardNo))
        };
    }

    /// <summary>Etiketləri sayır, saya görə azalan, sonra ada görə sıralayır.</summary>
    private static List<LabelCount> Tally(IEnumerable<string> items) =>
        items.Where(s => !string.IsNullOrWhiteSpace(s))
             .GroupBy(s => s)
             .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
             .OrderByDescending(x => x.Count)
             .ThenBy(x => x.Label, StringComparer.CurrentCulture)
             .ToList();
}
