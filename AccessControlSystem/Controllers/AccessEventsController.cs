using System.Globalization;
using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>
/// Keçid hadisələri — birbaşa Hikvision cihazının öz jurnalını (AcsEvent) sorğulayır.
/// Bütün hadisə tipləri (qapı, login, üz və s.), tarix+saat aralığı, server tərəfli pagination.
/// </summary>
public class AccessEventsController : Controller
{
    private readonly IHikvisionDeviceService _hik;
    private readonly IDeviceRepository _devices;
    private readonly HikvisionOptions _opt;

    public AccessEventsController(IHikvisionDeviceService hik, IDeviceRepository devices, HikvisionOptions opt)
    {
        _hik = hik;
        _devices = devices;
        _opt = opt;
    }

    public async Task<IActionResult> Index(string? empId, string? start, string? end)
    {
        ViewBag.Devices = await _devices.GetAllWithFloorAsync();
        var today = DateTime.Today;
        ViewBag.DefaultStart = ParseDateTime(start)?.ToString("yyyy-MM-ddTHH:mm:ss")
                               ?? today.ToString("yyyy-MM-ddTHH:mm:ss");
        ViewBag.DefaultEnd = ParseDateTime(end)?.ToString("yyyy-MM-ddTHH:mm:ss")
                             ?? today.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ss");
        ViewBag.EmpId = empId ?? "";
        return View();
    }

    /// <summary>Cihaz(lar)ın jurnalından bir səhifə (JSON) — filtr + pagination.
    /// deviceId ≤ 0 = bütün cihazlar; ad verilibsə qismən (substring) axtarış — hər ikisi aqreqasiya rejimi.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(long deviceId, string? start, string? end,
        int page = 1, int pageSize = 24, string? empId = null, string? name = null, string? card = null,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 24;

        var dto = new HikDeviceEventPageDto { Page = page, PageSize = pageSize };
        var startDt = ParseDateTime(start) ?? DateTime.Today;
        var endDt = ParseDateTime(end) ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        var from = new DateTimeOffset(startDt);
        var to = new DateTimeOffset(endDt);

        // Tək cihaz + adsız → cihazın öz pagination-ı (sürətli). Əks halda (bütün cihazlar VƏ YA qismən ad) → aqreqasiya.
        var aggregate = deviceId <= 0 || !string.IsNullOrWhiteSpace(name);

        if (!aggregate)
        {
            var d = await _devices.GetByIdAsync(deviceId);
            if (d is null) { dto.Error = "Cihaz seçilməyib və ya tapılmadı."; return Json(dto); }

            var target = ToHik(d);
            var position = (page - 1) * pageSize;
            var res = await _hik.SearchEventPageAsync(target, from, to, position, pageSize, empId, cardNo: card, ct: ct);
            if (!res.Ok) { dto.Error = res.Error ?? "Cihaz cavab vermədi."; return Json(dto); }

            dto.Total = res.Total;
            dto.TotalPages = res.Total > 0
                ? (int)Math.Ceiling(res.Total / (double)pageSize)
                : (res.Items.Count > 0 ? page : 0);

            dto.Items = res.Items.Select((r, idx) => Map(r, d.Id, position + idx + 1)).ToList();
            return Json(dto);
        }

        // Aqreqasiya: hədəf cihaz(lar)ı topla
        List<Device> devices;
        if (deviceId > 0)
        {
            var d = await _devices.GetByIdAsync(deviceId);
            if (d is null) { dto.Error = "Cihaz tapılmadı."; return Json(dto); }
            devices = new() { d };
        }
        else
        {
            devices = (await _devices.GetAllWithFloorAsync(ct)).Where(x => x.IsActive).ToList();
            if (devices.Count == 0) { dto.Error = "Aktiv cihaz yoxdur."; return Json(dto); }
        }

        // İşçi İD / Kart cihazın ÖZÜNDƏ süzülür (server-tərəfli) — beləliklə bütün aralıq üzrə
        // tam gəlir (əvvəl hamısını çəkib süzmək cap-a görə köhnə hadisələri itirirdi).
        var raws = await FetchAllAsync(devices, from, to, 5000, ct, empId, card);

        IEnumerable<(HikRawEvent e, Device d)> q = raws;
        if (!string.IsNullOrWhiteSpace(empId))
            q = q.Where(x => string.Equals((x.e.EmployeeNo ?? "").Trim(), empId.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(card))
            q = q.Where(x => string.Equals((x.e.CardNo ?? "").Trim(), card.Trim(), StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(name))
            q = q.Where(x => (x.e.Name ?? "").Contains(name.Trim(), StringComparison.OrdinalIgnoreCase));

        var ordered = q.OrderByDescending(x => x.e.Time ?? DateTimeOffset.MinValue).ToList();
        dto.Total = ordered.Count;
        dto.TotalPages = ordered.Count > 0 ? (int)Math.Ceiling(ordered.Count / (double)pageSize) : 0;

        var n = (page - 1) * pageSize;
        dto.Items = ordered.Skip(n).Take(pageSize).Select((x, idx) => Map(x.e, x.d.Id, n + idx + 1)).ToList();
        return Json(dto);
    }

    private HikDevice ToHik(Device d) =>
        new(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);

    private HikDeviceEventRow Map(HikRawEvent r, long devId, int no) => new()
    {
        No = no,
        EmployeeId = string.IsNullOrWhiteSpace(r.EmployeeNo) ? "--" : r.EmployeeNo!,
        Name = string.IsNullOrWhiteSpace(r.Name) ? "-" : r.Name!,
        CardNo = string.IsNullOrWhiteSpace(r.CardNo) ? "--" : r.CardNo!,
        EventType = HikEventTypes.Label(r.Major, r.Minor),
        Time = r.Time?.ToString("dd.MM.yyyy HH:mm:ss") ?? "-",
        PhotoUrl = string.IsNullOrWhiteSpace(r.PictureUrl)
            ? null
            : Url.Action("Photo", "AccessEvents", new { deviceId = devId, u = r.PictureUrl })
    };

    /// <summary>Bütün hədəf cihazların jurnalını aralıq üçün PARALEL çəkir. empId/card verilibsə
    /// cihaz özü süzür (bütün aralıq üzrə tam nəticə; cap yalnız süzülməmiş axtarışa aiddir).</summary>
    private async Task<List<(HikRawEvent e, Device d)>> FetchAllAsync(
        List<Device> devices, DateTimeOffset from, DateTimeOffset to, int cap, CancellationToken ct,
        string? empId = null, string? card = null)
    {
        var perDeviceCap = Math.Max(200, cap / Math.Max(1, devices.Count));
        var tasks = devices.Select(async d =>
        {
            var target = ToHik(d);
            var list = new List<(HikRawEvent, Device)>();
            var position = 0;
            for (var page = 0; page < 200; page++)
            {
                HikEventRawPage res;
                try { res = await _hik.SearchEventPageAsync(target, from, to, position, 100, empId, cardNo: card, ct: ct); }
                catch { break; }
                if (!res.Ok || res.Items.Count == 0) break;
                foreach (var e in res.Items) list.Add((e, d));
                position += res.Items.Count;
                if (res.Status != "MORE" || list.Count >= perDeviceCap) break;
            }
            return list;
        });

        var all = new List<(HikRawEvent, Device)>();
        foreach (var list in await Task.WhenAll(tasks)) all.AddRange(list);
        return all;
    }

    /// <summary>Cihazdakı hadisə snapshot-unu proxy edir (Digest auth cihaz tərəfdə).
    /// SSRF qorunması: yalnız həmin cihazın öz IP-sindən şəkil çəkilir.</summary>
    [HttpGet]
    public async Task<IActionResult> Photo(long deviceId, string? u, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(u)) return NotFound();
        var d = await _devices.GetByIdAsync(deviceId);
        if (d is null) return NotFound();

        // Mütləq URL-in host-u cihazın IP-si olmalıdır (daxili şəbəkəyə SSRF qarşısı).
        if (Uri.TryCreate(u, UriKind.Absolute, out var abs) &&
            !string.Equals(abs.Host, d.Ip, StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var target = new HikDevice(d.Ip, _opt.Username, _opt.Password,
            d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);
        var bytes = await _hik.DownloadPictureAsync(target, u, ct);
        if (bytes is null || bytes.Length == 0) return NotFound();
        return File(bytes, "image/jpeg");
    }

    /// <summary>Diaqnostika: aralıqdakı distinct (major/minor) kodlar + say + nümunə —
    /// cihazın real event tiplərini tutub etiket cədvəlini dəqiqləşdirmək üçün.</summary>
    [HttpGet]
    public async Task<IActionResult> Codes(long deviceId, string? start, string? end)
    {
        var d = await _devices.GetByIdAsync(deviceId);
        if (d is null) return Json(new { error = "Cihaz tapılmadı." });

        var startDt = ParseDateTime(start) ?? DateTime.Today;
        var endDt = ParseDateTime(end) ?? DateTime.Today.AddDays(1).AddSeconds(-1);
        var target = new HikDevice(d.Ip, _opt.Username, _opt.Password,
            d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);

        var seen = new Dictionary<(int?, int?), (int count, string label, string? sample)>();
        var position = 0;
        for (var p = 0; p < 20; p++) // ən çox ~2000 hadisə
        {
            var res = await _hik.SearchEventPageAsync(target, new DateTimeOffset(startDt), new DateTimeOffset(endDt),
                position, 100);
            if (!res.Ok || res.Items.Count == 0) break;
            foreach (var e in res.Items)
            {
                var key = (e.Major, e.Minor);
                if (seen.TryGetValue(key, out var cur))
                    seen[key] = (cur.count + 1, cur.label, cur.sample);
                else
                    seen[key] = (1, HikEventTypes.Label(e.Major, e.Minor), e.Name);
            }
            position += res.Items.Count;
            if (res.Status != "MORE") break;
        }

        return Json(seen
            .OrderByDescending(kv => kv.Value.count)
            .Select(kv => new { major = kv.Key.Item1, minor = kv.Key.Item2, count = kv.Value.count, label = kv.Value.label, sample = kv.Value.sample }));
    }

    /// <summary>datetime-local ("yyyy-MM-ddTHH:mm[:ss]") və ya boşluqlu formatı parse edir.</summary>
    private static DateTime? ParseDateTime(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return null;
        return DateTime.TryParse(s, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal, out var dt) ? dt : null;
    }
}
