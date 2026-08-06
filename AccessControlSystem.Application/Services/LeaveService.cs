using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;

namespace AccessControlSystem.Application.Services;

/// <summary>HR məzuniyyət/ezamiyyət/bayram idarəetməsi. Qeydlər birbaşa yaradılır
/// (self-service təsdiq axını sonra). Davamiyyət motoru bunları nəzərə alır.</summary>
public class LeaveService : ILeaveService
{
    private readonly ILeaveTypeRepository _types;
    private readonly ILeaveRecordRepository _records;
    private readonly IHolidayRepository _holidays;
    private readonly ICompanyRepository _companies;
    private readonly IEmployeeRepository _employees;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentTenant _tenant;
    private readonly ICurrentUserService _user;
    private readonly ISystemLogWriter _log;

    private static readonly string[] WeekdayAz = { "B", "B.e", "Ç.a", "Ç", "C.a", "C", "Ş" };

    public LeaveService(ILeaveTypeRepository types, ILeaveRecordRepository records, IHolidayRepository holidays,
        ICompanyRepository companies, IEmployeeRepository employees, IUnitOfWork uow,
        ICurrentTenant tenant, ICurrentUserService user, ISystemLogWriter log)
    {
        _types = types; _records = records; _holidays = holidays;
        _companies = companies; _employees = employees; _uow = uow;
        _tenant = tenant; _user = user; _log = log;
    }

    private long? OwnerCompanyId(long? formCompanyId = null) =>
        _tenant.IsGlobalAdmin ? formCompanyId : _tenant.CompanyId;

    // ---------- Növlər ----------

    public async Task<List<LeaveTypeItemDto>> GetTypesAsync(CancellationToken ct = default)
    {
        var list = await _types.GetAllWithCompanyAsync(ct);
        var result = new List<LeaveTypeItemDto>();
        foreach (var t in list)
            result.Add(new LeaveTypeItemDto
            {
                Id = t.Id, Name = t.Name, CountsAsWorked = t.CountsAsWorked, Paid = t.Paid,
                Color = string.IsNullOrWhiteSpace(t.Color) ? "#8b5cf6" : t.Color!,
                CompanyName = t.Company?.Name ?? "Qlobal",
                UsageCount = await _types.UsageCountAsync(t.Id, ct),
                IsActive = t.IsActive
            });
        return result;
    }

    public async Task<List<LookupDto>> GetTypeLookupAsync(CancellationToken ct = default) =>
        (await _types.GetActiveAsync(ct)).Select(t => new LookupDto
        {
            Id = t.Id,
            Name = t.CountsAsWorked ? $"{t.Name} (işdə sayılır)" : t.Name
        }).ToList();

    public async Task<long> AddTypeAsync(LeaveTypeInputDto dto, CancellationToken ct = default)
    {
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Növün adını daxil edin.");
        var t = new LeaveType
        {
            Name = name, CountsAsWorked = dto.CountsAsWorked, Paid = dto.Paid,
            Color = string.IsNullOrWhiteSpace(dto.Color) ? "#8b5cf6" : dto.Color.Trim(),
            CompanyId = OwnerCompanyId(dto.CompanyId), IsActive = true
        };
        await _types.AddAsync(t, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LEAVETYPE_CREATED", $"{t.Name} növü əlavə edildi.", "leavetype", t.Id, ct: ct);
        return t.Id;
    }

    public async Task ToggleTypeAsync(long id, CancellationToken ct = default)
    {
        var t = await _types.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Növ tapılmadı.");
        t.IsActive = !t.IsActive; t.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LEAVETYPE_STATUS_CHANGED",
            $"{t.Name} növü {(t.IsActive ? "aktiv" : "deaktiv")} edildi.", "leavetype", t.Id, ct: ct);
    }

    public async Task DeleteTypeAsync(long id, CancellationToken ct = default)
    {
        var t = await _types.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Növ tapılmadı.");
        if (await _types.UsageCountAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu növ qeydlərdə istifadə olunub — silinə bilməz (deaktiv edin).");
        var name = t.Name;
        _types.Remove(t);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LEAVETYPE_DELETED", $"{name} növü silindi.", "leavetype", id, ct: ct);
    }

    // ---------- İşçi qeydləri ----------

    public async Task<List<LeaveRecordItemDto>> GetRecordsAsync(DateTime from, DateTime to,
        long? companyId, long? deptId, CancellationToken ct = default)
    {
        var list = await _records.GetForRangeAsync(from.Date, to.Date, ct);
        IEnumerable<LeaveRecord> q = list;
        if (companyId is { } c) q = q.Where(r => r.CompanyId == c);
        if (deptId is { } d) q = q.Where(r => r.Employee?.DepartmentId == d);
        return q.Select(r => new LeaveRecordItemDto
        {
            Id = r.Id,
            EmployeeName = r.Employee?.FullName ?? "—",
            EmployeeNo = r.Employee?.EmployeeNo ?? "",
            Department = r.Employee?.Department?.Name,
            TypeName = r.LeaveType?.Name ?? "—",
            CountsAsWorked = r.LeaveType?.CountsAsWorked ?? false,
            Color = string.IsNullOrWhiteSpace(r.LeaveType?.Color) ? "#8b5cf6" : r.LeaveType!.Color!,
            StartDate = r.StartDate.ToString("dd.MM.yyyy"),
            EndDate = r.EndDate.ToString("dd.MM.yyyy"),
            Days = (int)(r.EndDate.Date - r.StartDate.Date).TotalDays + 1,
            Reason = r.Reason
        }).ToList();
    }

    public async Task<long> AddRecordAsync(LeaveRecordInputDto dto, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdAsync(dto.EmployeeId, ct)
                  ?? throw new ArgumentException("İşçi seçilməlidir.");
        if (await _types.GetByIdAsync(dto.LeaveTypeId, ct) is null)
            throw new ArgumentException("Növ seçilməlidir.");
        if (!DateTime.TryParse(dto.StartDate, out var start) || !DateTime.TryParse(dto.EndDate, out var end))
            throw new ArgumentException("Tarixləri düzgün daxil edin.");
        if (end.Date < start.Date)
            throw new ArgumentException("Bitmə tarixi başlanğıcdan əvvəl ola bilməz.");

        var rec = new LeaveRecord
        {
            CompanyId = emp.CompanyId,
            EmployeeId = emp.Id,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = start.Date,
            EndDate = end.Date,
            Reason = dto.Reason?.Trim(),
            CreatedByUserId = _user.UserId
        };
        await _records.AddAsync(rec, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LEAVE_CREATED",
            $"{emp.FullName}: {start:dd.MM.yyyy}–{end:dd.MM.yyyy} qeyd əlavə edildi.", "leave", rec.Id, ct: ct);
        return rec.Id;
    }

    public async Task DeleteRecordAsync(long id, CancellationToken ct = default)
    {
        var r = await _records.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Qeyd tapılmadı.");
        _records.Remove(r);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LEAVE_DELETED", "Məzuniyyət/ezamiyyət qeydi silindi.", "leave", id, ct: ct);
    }

    // ---------- Bayramlar ----------

    public async Task<List<HolidayItemDto>> GetHolidaysAsync(int year, CancellationToken ct = default)
    {
        var from = new DateTime(year, 1, 1);
        var to = new DateTime(year, 12, 31);
        var all = (await _holidays.GetForRangeAsync(from, to, ct))
            .OrderBy(h => h.CompanyId).ThenBy(h => h.Date).ToList();

        // Ardıcıl eyni ad + eyni şirkət günlərini aralıq kimi birləşdir.
        var result = new List<HolidayItemDto>();
        int i = 0;
        while (i < all.Count)
        {
            var head = all[i];
            var ids = new List<long> { head.Id };
            int j = i;
            while (j + 1 < all.Count
                   && all[j + 1].Name == head.Name
                   && all[j + 1].CompanyId == head.CompanyId
                   && all[j + 1].Date == all[j].Date.AddDays(1))
            {
                j++;
                ids.Add(all[j].Id);
            }
            var start = head.Date;
            var end = all[j].Date;
            result.Add(new HolidayItemDto
            {
                Ids = string.Join(",", ids),
                RangeLabel = start == end ? start.ToString("dd.MM.yyyy") : $"{start:dd.MM.yyyy} – {end:dd.MM.yyyy}",
                Days = ids.Count,
                Name = head.Name,
                CompanyName = head.Company?.Name ?? "Qlobal"
            });
            i = j + 1;
        }
        return result;
    }

    public async Task<long> AddHolidayAsync(HolidayInputDto dto, CancellationToken ct = default)
    {
        if (!DateTime.TryParse(dto.StartDate, out var start))
            throw new ArgumentException("Başlanğıc tarixi düzgün daxil edin.");
        var end = DateTime.TryParse(dto.EndDate, out var e) ? e : start;
        if (end.Date < start.Date)
            throw new ArgumentException("Bitmə tarixi başlanğıcdan əvvəl ola bilməz.");
        var name = (dto.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Bayramın adını daxil edin.");
        var owner = OwnerCompanyId(dto.CompanyId);

        // Aralıqdakı hər gün üçün ayrıca bayram günü yaradılır (mövcud olanlar keçilir).
        var added = 0;
        long firstId = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (await _holidays.ExistsAsync(owner, d, ct)) continue;
            var h = new Holiday { Date = d, Name = name, CompanyId = owner };
            await _holidays.AddAsync(h, ct);
            await _uow.SaveChangesAsync(ct);
            if (firstId == 0) firstId = h.Id;
            added++;
        }
        if (added == 0) throw new ArgumentException("Bu aralıqdakı günlər üçün bayram artıq mövcuddur.");
        await _log.LogAsync("HOLIDAY_CREATED",
            $"{start:dd.MM.yyyy}–{end:dd.MM.yyyy} — {name} ({added} gün) bayramı əlavə edildi.", "holiday", firstId, ct: ct);
        return firstId;
    }

    public async Task DeleteHolidaysAsync(IEnumerable<long> ids, CancellationToken ct = default)
    {
        var list = ids?.Distinct().ToList() ?? new List<long>();
        if (list.Count == 0) throw new ArgumentException("Silinəcək bayram seçilməyib.");
        string? name = null;
        foreach (var id in list)
        {
            var h = await _holidays.GetByIdAsync(id, ct);
            if (h is null) continue;
            name ??= h.Name;
            _holidays.Remove(h);
        }
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("HOLIDAY_DELETED", $"{name ?? "Bayram"} ({list.Count} gün) silindi.", "holiday", null, ct: ct);
    }
}
