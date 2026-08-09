using System.Globalization;
using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

/// <summary>Davamiyyət hesablama motoru — cihazlardan xam keçidləri çəkir, işçinin
/// effektiv iş cədvəli ilə tutuşdurur və gündəlik nəticə (gecikmə/erkən çıxış/işlənmiş
/// saat/status) çıxarır. Nəticələr indilik anlıq hesablanır (persist yoxdur — Faza 5).
/// Bax: docs/ATTENDANCE_DESIGN.md</summary>
public class AttendanceService : IAttendanceService
{
    private readonly IEmployeeRepository _employees;
    private readonly IDeviceRepository _devices;
    private readonly IHikvisionDeviceService _hik;
    private readonly HikvisionOptions _opt;
    private readonly IHolidayRepository _holidays;
    private readonly ILeaveRecordRepository _leaves;

    private static readonly string[] WeekdayAz = { "B", "B.e", "Ç.a", "Ç", "C.a", "C", "Ş" }; // index = (int)DayOfWeek
    private static readonly string[] MonthAz =
        { "yanvar", "fevral", "mart", "aprel", "may", "iyun",
          "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };

    public AttendanceService(IEmployeeRepository employees, IDeviceRepository devices,
        IHikvisionDeviceService hik, HikvisionOptions opt,
        IHolidayRepository holidays, ILeaveRecordRepository leaves)
    {
        _employees = employees;
        _devices = devices;
        _hik = hik;
        _opt = opt;
        _holidays = holidays;
        _leaves = leaves;
    }

    /// <summary>Bir günü əhatə edən məzuniyyət/ezamiyyət qeydi.</summary>
    private readonly record struct LeaveSpan(DateTime Start, DateTime End, bool CountsAsWorked);

    // ---------------- Gündəlik ----------------

    public async Task<AttendanceDailyDto> GetDailyAsync(long? companyId, long? departmentId, long? employeeId,
        DateTime from, DateTime to, string? kind = null, CancellationToken ct = default)
    {
        var fromD = from.Date;
        var toD = to.Date;
        var data = await LoadAsync(companyId, departmentId, employeeId, fromD, toD, ct);
        var employees = data.Employees;

        // 1) İşçi × gün — xam nəticələr.
        var items = new List<(Employee e, DateTime day, DayResult r)>();
        foreach (var e in employees)
        {
            var ws = e.WorkSchedule ?? e.Department?.WorkSchedule;
            var scans = data.Scans.TryGetValue(e.Id, out var s) ? s : new();
            for (var day = fromD; day <= toD; day = day.AddDays(1))
                items.Add((e, day, Compute(ws, day, scans, data.Holidays.Contains(day.Date), LeaveOn(data.Leaves, e.Id, day))));
        }

        // 2) Hesabat növünə görə süz.
        var k = (kind ?? "").Trim().ToLowerInvariant();
        Func<(Employee e, DateTime day, DayResult r), bool> keep = k switch
        {
            "late" => x => x.r.Status == "late",
            "early" => x => x.r.Status == "early",
            "absent" => x => x.r.Status == "absent",
            "incomplete" => x => x.r.Status == "incomplete",
            "overtime" => x => x.r.OvertimeMin > 0,
            "leave" => x => x.r.Status == "leave",
            "trip" => x => x.r.Status == "trip",
            _ => _ => true
        };
        var filtered = items.Where(keep).ToList();

        // 3) Ardıcıl eyni statuslu "düz" günləri (bayram/məzuniyyət/ezamiyyət/istirahət/
        //    qayıb/cədvəlsiz) tək aralıq sətrində birləşdir — siyahı şişməsin.
        var flat = new HashSet<string> { "holiday", "leave", "trip", "rest", "absent", "noschedule", "future" };
        var rows = new List<AttDayRowDto>();
        foreach (var g in filtered.GroupBy(x => x.e.Id))
        {
            var ordered = g.OrderBy(x => x.day).ToList();
            int i = 0;
            while (i < ordered.Count)
            {
                var cur = ordered[i];
                if (flat.Contains(cur.r.Status))
                {
                    int j = i;
                    while (j + 1 < ordered.Count && ordered[j + 1].r.Status == cur.r.Status
                           && ordered[j + 1].day == ordered[j].day.AddDays(1))
                        j++;
                    rows.Add(BuildRow(cur.e, cur.r, cur.day, ordered[j].day, j - i + 1));
                    i = j + 1;
                }
                else
                {
                    rows.Add(BuildRow(cur.e, cur.r, cur.day, cur.day, 1));
                    i++;
                }
            }
        }

        var totOver = filtered.Sum(x => x.r.OvertimeMin);
        return new AttendanceDailyDto
        {
            FromLabel = fromD.ToString("dd.MM.yyyy"),
            ToLabel = toD.ToString("dd.MM.yyyy"),
            Scope = Scope(companyId, departmentId, employees),
            Kind = k,
            KindLabel = KindLabel(k),
            TotalEmployees = employees.Count,
            TotalLate = filtered.Count(x => x.r.Status == "late"),
            TotalAbsent = filtered.Count(x => x.r.Status == "absent"),
            TotalOvertimeMin = totOver,
            TotalOvertimeHm = Hm(totOver),
            Rows = rows
        };
    }

    /// <summary>Bir sətir (tək gün və ya aralıq) qurur. days>1 olduqda vaxt sütunları boş.</summary>
    private AttDayRowDto BuildRow(Employee e, DayResult r, DateTime startDay, DateTime endDay, int days)
    {
        var isRange = days > 1;
        return new AttDayRowDto
        {
            EmployeeNo = e.EmployeeNo,
            FullName = e.FullName,
            Department = e.Department?.Name,
            Date = isRange ? $"{startDay:dd.MM.yyyy} – {endDay:dd.MM.yyyy}" : startDay.ToString("dd.MM.yyyy"),
            Weekday = isRange ? $"{days} gün" : WeekdayAz[(int)startDay.DayOfWeek],
            Days = days,
            Schedule = r.Schedule,
            In = isRange ? null : r.In?.ToString("HH:mm"),
            Out = isRange ? null : r.Out?.ToString("HH:mm"),
            LateMin = isRange ? 0 : r.LateMin,
            EarlyMin = isRange ? 0 : r.EarlyMin,
            OvertimeMin = isRange ? 0 : r.OvertimeMin,
            WorkedMin = isRange ? 0 : r.WorkedMin,
            WorkedHm = isRange ? "0" : Hm(r.WorkedMin),
            OvertimeHm = isRange ? "0" : Hm(r.OvertimeMin),
            Status = r.Status,
            StatusLabel = StatusLabel(r.Status)
        };
    }

    private static string KindLabel(string k) => k switch
    {
        "late" => "Gecikənlər",
        "early" => "Erkən çıxanlar",
        "absent" => "Gəlməyənlər",
        "incomplete" => "Anomaliya (natamam)",
        "overtime" => "Əlavə iş (overtime)",
        "leave" => "Məzuniyyət",
        "trip" => "Ezamiyyət",
        _ => "Bütün günlər"
    };

    // ---------------- İşçi üzrə yekun ----------------

    public async Task<AttendanceSummaryDto> GetSummaryAsync(long? companyId, long? departmentId, long? employeeId,
        DateTime from, DateTime to, string? kind = null, CancellationToken ct = default)
    {
        var fromD = from.Date;
        var toD = to.Date;
        var data = await LoadAsync(companyId, departmentId, employeeId, fromD, toD, ct);

        var rows = new List<AttSumRowDto>();
        foreach (var e in data.Employees)
        {
            var ws = e.WorkSchedule ?? e.Department?.WorkSchedule;
            var scans = data.Scans.TryGetValue(e.Id, out var s) ? s : new();
            var row = new AttSumRowDto { EmployeeNo = e.EmployeeNo, FullName = e.FullName, Department = e.Department?.Name };

            for (var day = fromD; day <= toD; day = day.AddDays(1))
            {
                var r = Compute(ws, day, scans, data.Holidays.Contains(day.Date), LeaveOn(data.Leaves, e.Id, day));
                switch (r.Status)
                {
                    case "normal": case "late": case "early": case "incomplete": case "trip": row.PresentDays++; break;
                    case "absent": row.AbsentDays++; break;
                    case "leave": row.LeaveDays++; break;
                    case "holiday": row.HolidayDays++; break;
                }
                if (r.Status == "late") { row.LateDays++; row.LateMin += r.LateMin; }
                if (r.Status == "early") row.EarlyDays++;
                if (r.Status == "incomplete") row.IncompleteDays++;
                if (r.Status == "trip") row.TripDays++;
                row.OvertimeMin += r.OvertimeMin;
                row.WorkedMin += r.WorkedMin;
            }
            row.WorkedHm = Hm(row.WorkedMin);
            row.OvertimeHm = Hm(row.OvertimeMin);
            rows.Add(row);
        }

        // Hesabat növünə görə YALNIZ uyğun işçiləri saxla (məs. gecikənlər = LateDays>0).
        var k = (kind ?? "").Trim().ToLowerInvariant();
        rows = k switch
        {
            "late" => rows.Where(x => x.LateDays > 0).ToList(),
            "early" => rows.Where(x => x.EarlyDays > 0).ToList(),
            "absent" => rows.Where(x => x.AbsentDays > 0).ToList(),
            "incomplete" => rows.Where(x => x.IncompleteDays > 0).ToList(),
            "overtime" => rows.Where(x => x.OvertimeMin > 0).ToList(),
            "leave" => rows.Where(x => x.LeaveDays > 0).ToList(),
            "trip" => rows.Where(x => x.TripDays > 0).ToList(),
            _ => rows
        };

        return new AttendanceSummaryDto
        {
            FromLabel = fromD.ToString("dd.MM.yyyy"),
            ToLabel = toD.ToString("dd.MM.yyyy"),
            Scope = Scope(companyId, departmentId, data.Employees),
            KindLabel = SummaryKindLabel(k),
            TotalEmployees = rows.Count,
            Rows = rows
        };
    }

    private static string SummaryKindLabel(string k) => k switch
    {
        "late" => "Gecikən işçilər",
        "early" => "Erkən çıxan işçilər",
        "absent" => "Gəlməyən işçilər",
        "incomplete" => "Natamam (anomaliya) olan işçilər",
        "overtime" => "Əlavə iş görən işçilər",
        "leave" => "Məzuniyyətli işçilər",
        "trip" => "Ezamiyyətli işçilər",
        _ => "Bütün işçilər"
    };

    // ---------------- Aylıq kart ----------------

    public async Task<AttendanceMonthlyDto> GetMonthlyAsync(long? companyId, long? departmentId, long? employeeId,
        int year, int month, CancellationToken ct = default)
    {
        if (month < 1 || month > 12) month = DateTime.Today.Month;
        var fromD = new DateTime(year, month, 1);
        var toD = fromD.AddMonths(1).AddDays(-1);
        var data = await LoadAsync(companyId, departmentId, employeeId, fromD, toD, ct);
        var employees = data.Employees;

        var days = Enumerable.Range(1, DateTime.DaysInMonth(year, month)).ToList();
        var rows = new List<AttMonthRowDto>();

        foreach (var e in employees)
        {
            var ws = e.WorkSchedule ?? e.Department?.WorkSchedule;
            var scans = data.Scans.TryGetValue(e.Id, out var s) ? s : new();
            var row = new AttMonthRowDto { EmployeeNo = e.EmployeeNo, FullName = e.FullName, Department = e.Department?.Name };

            foreach (var d in days)
            {
                var day = new DateTime(year, month, d);
                var r = Compute(ws, day, scans, data.Holidays.Contains(day.Date), LeaveOn(data.Leaves, e.Id, day));
                if (r.Status is "normal" or "late" or "early" or "incomplete" or "trip") row.PresentDays++;
                if (r.Status == "absent") row.AbsentDays++;
                if (r.Status == "late") row.LateCount++;
                row.WorkedMin += r.WorkedMin;
                row.Cells.Add(new AttCellDto
                {
                    Day = d,
                    Status = r.Status,
                    Text = CellText(r),
                    Title = CellTitle(day, r)
                });
            }
            row.WorkedHm = Hm(row.WorkedMin);
            rows.Add(row);
        }

        return new AttendanceMonthlyDto
        {
            Year = year,
            Month = month,
            MonthLabel = $"{MonthAz[month - 1]} {year}",
            Scope = Scope(companyId, departmentId, employees),
            Days = days,
            Rows = rows
        };
    }

    // ---------------- Ortaq: yükləmə ----------------

    private sealed class LoadResult
    {
        public List<Employee> Employees = new();
        public Dictionary<long, List<(DateTime time, bool isExit)>> Scans = new();
        public HashSet<DateTime> Holidays = new();
        public Dictionary<long, List<LeaveSpan>> Leaves = new();
    }

    /// <summary>Süzülmüş işçilər + xam skanlar + bayram günləri + məzuniyyət/ezamiyyət qeydləri.</summary>
    private async Task<LoadResult> LoadAsync(long? companyId, long? departmentId, long? employeeId,
        DateTime fromD, DateTime toD, CancellationToken ct)
    {
        var all = await _employees.GetAllAsync(ct);
        var matcher = new EmployeeMatcher(all);

        // Son gün üçün gün-sərhədi (məs. 05:00) növbəti gün səhərinə qədər uzana bilir —
        // ona görə +2 gün çəkirik (motor pəncərəyə görə süzür).
        var raws = await FetchRawScansAsync(
            new DateTimeOffset(fromD), new DateTimeOffset(toD.AddDays(2).AddSeconds(-1)), ct);

        var byEmp = new Dictionary<long, List<(DateTime, bool)>>();
        foreach (var r in raws)
        {
            var id = matcher.Resolve(r.deviceId, r.number, r.name);
            if (id is null) continue;
            if (!byEmp.TryGetValue(id.Value, out var l)) byEmp[id.Value] = l = new();
            l.Add((r.time, r.isExit));
        }

        var holidays = (await _holidays.GetDatesAsync(fromD, toD, ct)).Select(d => d.Date).ToHashSet();

        var leavesByEmp = new Dictionary<long, List<LeaveSpan>>();
        foreach (var lr in await _leaves.GetForRangeAsync(fromD, toD, ct))
        {
            if (!leavesByEmp.TryGetValue(lr.EmployeeId, out var l)) leavesByEmp[lr.EmployeeId] = l = new();
            l.Add(new LeaveSpan(lr.StartDate.Date, lr.EndDate.Date, lr.LeaveType?.CountsAsWorked ?? false));
        }

        var employees = all.AsEnumerable();
        if (companyId is { } c) employees = employees.Where(e => e.CompanyId == c);
        if (departmentId is { } d) employees = employees.Where(e => e.DepartmentId == d);
        if (employeeId is { } eid) employees = employees.Where(e => e.Id == eid);

        return new LoadResult
        {
            Employees = employees.OrderBy(e => e.LastName).ThenBy(e => e.FirstName).ToList(),
            Scans = byEmp,
            Holidays = holidays,
            Leaves = leavesByEmp
        };
    }

    /// <summary>Verilmiş günü əhatə edən məzuniyyət qeydini tapır (yoxdursa null).</summary>
    private static LeaveSpan? LeaveOn(Dictionary<long, List<LeaveSpan>> leaves, long empId, DateTime day)
    {
        if (!leaves.TryGetValue(empId, out var list)) return null;
        foreach (var s in list)
            if (day.Date >= s.Start && day.Date <= s.End) return s;
        return null;
    }

    // ---------------- Ortaq: bir günün hesablanması ----------------

    private readonly record struct DayResult(
        string Schedule, DateTime? In, DateTime? Out, int LateMin, int EarlyMin, int OvertimeMin, int WorkedMin, string Status);

    private static DayResult Compute(WorkSchedule? ws, DateTime day, List<(DateTime time, bool isExit)> scans,
        bool isHoliday, LeaveSpan? leave)
    {
        // İştirak günü pəncərəsi: [gün + DayStart, növbəti gün + DayStart).
        // Beləliklə gecə oxutmaları (məs. 00:17) əvvəlki günə düşür, bu günə qarışmır.
        var winStart = day.Date + (ws?.DayStartTime ?? TimeSpan.Zero);
        var winEnd = winStart.AddDays(1);
        var dayScans = scans.Where(x => x.time >= winStart && x.time < winEnd).ToList();
        var entries = dayScans.Where(x => !x.isExit).Select(x => x.time).ToList();
        var exits = dayScans.Where(x => x.isExit).Select(x => x.time).ToList();
        var allTimes = dayScans.Select(x => x.time).OrderBy(t => t).ToList();

        // İlk giriş: giriş cihazından, yoxsa günün ilk skanı. Son çıxış: çıxış cihazından,
        // yoxsa (birdən çox skan varsa) günün son skanı.
        DateTime? firstIn = entries.Count > 0 ? entries.Min() : (allTimes.Count > 0 ? allTimes.First() : null);
        DateTime? lastOut = exits.Count > 0 ? exits.Max() : (allTimes.Count > 1 ? allTimes.Last() : null);

        var worked0 = (firstIn is { } a && lastOut is { } b) ? (int)Math.Round((b - a).TotalMinutes) : 0;

        // Prioritet: bayram → məzuniyyət/ezamiyyət → cədvəlsiz → istirahət → hesablama.
        if (isHoliday)
            return new DayResult("Bayram", firstIn, lastOut, 0, 0, 0, worked0, "holiday");
        if (leave is { } lv)
            return new DayResult(lv.CountsAsWorked ? "Ezamiyyət" : "Məzuniyyət",
                firstIn, lastOut, 0, 0, 0, worked0, lv.CountsAsWorked ? "trip" : "leave");

        if (ws is null)
            return new DayResult("—", firstIn, lastOut, 0, 0, 0, 0, "noschedule");
        if (!ws.IsWorkDay(day.DayOfWeek))
            return new DayResult("İstirahət", firstIn, lastOut, 0, 0, 0, 0, "rest");

        var sched = $"{ws.StartTime:hh\\:mm}–{ws.EndTime:hh\\:mm}";
        // Gələcək iş günü (hələ gəlməyib) — qayıb sayılmır, boş göstərilir.
        if (day.Date > DateTime.Today && firstIn is null && lastOut is null)
            return new DayResult(sched, null, null, 0, 0, 0, 0, "future");
        if (firstIn is null && lastOut is null)
            return new DayResult(sched, null, null, 0, 0, 0, 0, "absent");

        var worked = (firstIn is { } fi && lastOut is { } lo) ? (int)Math.Round((lo - fi).TotalMinutes) : 0;
        var schedEnd = day.Date + ws.EndTime;
        // Əlavə iş (overtime): plan bitməsindən sonra işlənən vaxt (çıxış varsa).
        var over = lastOut is { } lo3 ? Math.Max(0, (int)Math.Round((lo3 - schedEnd).TotalMinutes)) : 0;

        if (ws.Type == TimetableType.Flexible)
        {
            var st = worked < ws.MinWorkMinutes ? "incomplete" : "normal";
            return new DayResult(sched, firstIn, lastOut, 0, 0, over, worked, st);
        }

        var schedStart = day.Date + ws.StartTime;
        var late = firstIn is { } f ? Math.Max(0, (int)Math.Round((f - schedStart).TotalMinutes) - ws.GraceMinutes) : 0;
        var early = lastOut is { } l ? Math.Max(0, (int)Math.Round((schedEnd - l).TotalMinutes) - ws.EarlyLeaveGraceMinutes) : 0;

        string status;
        if (lastOut is null) status = "incomplete";
        else if (late > 0) status = "late";
        else if (early > 0) status = "early";
        else status = "normal";

        return new DayResult(sched, firstIn, lastOut, late, early, over, worked, status);
    }

    // ---------------- Formatlaşdırma ----------------

    private static string Hm(int minutes)
    {
        if (minutes <= 0) return "0";
        return $"{minutes / 60}s {minutes % 60}d";
    }

    private static string StatusLabel(string s) => s switch
    {
        "normal" => "Vaxtında",
        "late" => "Gecikdi",
        "early" => "Erkən çıxdı",
        "absent" => "Gəlmədi",
        "incomplete" => "Natamam",
        "rest" => "İstirahət",
        "holiday" => "Bayram",
        "leave" => "Məzuniyyət",
        "trip" => "Ezamiyyət",
        "future" => "—",
        _ => "Cədvəlsiz"
    };

    private static string CellText(DayResult r) => r.Status switch
    {
        "normal" => "•",
        "late" => "G",
        "early" => "E",
        "absent" => "Q",
        "incomplete" => "N",
        "rest" => "–",
        "holiday" => "B",
        "leave" => "M",
        "trip" => "Z",
        "future" => "",
        _ => ""
    };

    private static string CellTitle(DateTime day, DayResult r)
    {
        var head = $"{day:dd.MM} · {StatusLabel(r.Status)}";
        if (r.In is null && r.Out is null) return head;
        var io = $"{r.In?.ToString("HH:mm") ?? "—"}–{r.Out?.ToString("HH:mm") ?? "—"}";
        var extra = r.LateMin > 0 ? $" · gecikmə {r.LateMin}d" : (r.EarlyMin > 0 ? $" · erkən {r.EarlyMin}d" : "");
        return $"{head} · {io}{extra}";
    }

    private static string Scope(long? companyId, long? departmentId, List<Employee> employees)
    {
        var company = companyId is null ? "Bütün müəssisələr"
            : employees.FirstOrDefault()?.Company?.Name ?? "Müəssisə";
        var dept = departmentId is null ? "bütün şöbələr"
            : employees.FirstOrDefault()?.Department?.Name ?? "şöbə";
        return $"{company} · {dept}";
    }

    // ---------------- Cihazdan xam çəkmə + gün-gün keş ----------------

    private readonly record struct RawScan(long deviceId, string number, string? name, DateTime time, bool isExit);

    // Prosesboyu keş: (cihaz, təqvim-günü) → o günün oxutmaları. Keçmiş günlər dəyişmədiyi üçün
    // uzun saxlanır (bir dəfə çəkilir), bu gün/gələcək qısa (yeni oxutma gələ bilər).
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime exp, List<RawScan> scans)> _scanCache = new();

    private static string ScanKey(long deviceId, DateTime day) => $"{deviceId}:{day:yyyyMMdd}";
    private static TimeSpan CacheTtl(DateTime day) =>
        day.Date < DateTime.Today ? TimeSpan.FromHours(6) : TimeSpan.FromSeconds(90);

    private async Task<List<RawScan>> FetchRawScansAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var devices = (await _devices.GetAllWithFloorAsync(ct)).Where(x => x.IsActive).ToList();
        var fromDate = from.Date;
        var toDate = to.Date;
        // Vaxtaşırı vaxtı keçmiş keş girişlərini təmizlə (sonsuz böyüməsin).
        if (_scanCache.Count > 4000)
        {
            var now0 = DateTime.Now;
            foreach (var kv in _scanCache)
                if (kv.Value.exp <= now0) _scanCache.TryRemove(kv.Key, out _);
        }
        var perDevice = await Task.WhenAll(devices.Select(d => FetchDeviceCachedAsync(d, fromDate, toDate, ct)));
        return perDevice.SelectMany(x => x).ToList();
    }

    /// <summary>Bir cihaz üçün [fromDate, toDate] oxutmalarını keşdən + çatmayan günləri cihazdan çəkir.</summary>
    private async Task<List<RawScan>> FetchDeviceCachedAsync(Device d, DateTime fromDate, DateTime toDate, CancellationToken ct)
    {
        var result = new List<RawScan>();
        var missing = new List<DateTime>();
        var now = DateTime.Now;
        for (var day = fromDate; day <= toDate; day = day.AddDays(1))
        {
            if (_scanCache.TryGetValue(ScanKey(d.Id, day), out var e) && e.exp > now)
                result.AddRange(e.scans);
            else
                missing.Add(day);
        }
        if (missing.Count > 0)
        {
            var lo = missing.Min();
            var hi = missing.Max();
            var fetched = await FetchOneDeviceRawAsync(d,
                new DateTimeOffset(lo), new DateTimeOffset(hi.AddDays(1).AddSeconds(-1)), ct);
            var byDay = fetched.GroupBy(s => s.time.Date).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var day in missing)
            {
                var dayScans = byDay.TryGetValue(day.Date, out var l) ? l : new List<RawScan>();
                _scanCache[ScanKey(d.Id, day)] = (DateTime.Now.Add(CacheTtl(day)), dayScans);
                result.AddRange(dayScans);
            }
        }
        return result;
    }

    private async Task<List<RawScan>> FetchOneDeviceRawAsync(Device d, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
    {
        var result = new List<RawScan>();
        var target = new HikDevice(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);
        var isExit = (d.AccessPoint?.Direction ?? d.Direction) == DeviceDirection.Exit;
        var position = 0;

        // Cihaz səviyyəsində YALNIZ üz təsdiqi (major 5 / minor 75) çəkilir. Qapı açıldı/bağlandı
        // və s. səs-küy gəlmir — uzun aralıqda (aylıq) hadisə sayı limitə çatıb köhnə günləri
        // İTİRMİR (əvvəl bütün hadisələr çəkilirdi, cap 20000-ə çatanda köhnə günlər düşürdü).
        for (var page = 0; page < 500; page++)
        {
            HikEventRawPage res;
            try { res = await _hik.SearchEventPageAsync(target, from, to, position, 100, major: 5, minor: 75, ct: ct); }
            catch { break; }
            if (!res.Ok || res.Items.Count == 0) break;

            foreach (var e in res.Items)
            {
                if (string.IsNullOrWhiteSpace(e.EmployeeNo)) continue;
                if (e.Minor is not (1 or 75)) continue;   // yalnız uğurlu keçid (kart/üz), 76 uğursuz üz yox
                if (e.Time is not { } t) continue;
                result.Add(new RawScan(d.Id, e.EmployeeNo!.Trim(), e.Name, t.LocalDateTime, isExit));
            }

            position += res.Items.Count;
            if (res.Status != "MORE") break;
        }
        return result;
    }
}
