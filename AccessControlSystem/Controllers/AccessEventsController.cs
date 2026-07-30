using System.Globalization;
using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
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

    public async Task<IActionResult> Index()
    {
        ViewBag.Devices = await _devices.GetAllWithFloorAsync();
        var today = DateTime.Today;
        ViewBag.DefaultStart = today.ToString("yyyy-MM-ddTHH:mm:ss");
        ViewBag.DefaultEnd = today.AddDays(1).AddSeconds(-1).ToString("yyyy-MM-ddTHH:mm:ss");
        return View();
    }

    /// <summary>Cihazın jurnalından bir səhifə (JSON) — filtr + pagination.</summary>
    [HttpGet]
    public async Task<IActionResult> Search(long deviceId, string? start, string? end,
        int page = 1, int pageSize = 24, string? empId = null, string? name = null, string? card = null)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 24;

        var dto = new HikDeviceEventPageDto { Page = page, PageSize = pageSize };

        var d = await _devices.GetByIdAsync(deviceId);
        if (d is null) { dto.Error = "Cihaz seçilməyib və ya tapılmadı."; return Json(dto); }

        var startDt = ParseDateTime(start) ?? DateTime.Today;
        var endDt = ParseDateTime(end) ?? DateTime.Today.AddDays(1).AddSeconds(-1);

        var target = new HikDevice(d.Ip, _opt.Username, _opt.Password,
            d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);
        var position = (page - 1) * pageSize;

        var res = await _hik.SearchEventPageAsync(target, new DateTimeOffset(startDt), new DateTimeOffset(endDt),
            position, pageSize, empId, name, card);
        if (!res.Ok) { dto.Error = res.Error ?? "Cihaz cavab vermədi."; return Json(dto); }

        dto.Total = res.Total;
        dto.TotalPages = res.Total > 0
            ? (int)Math.Ceiling(res.Total / (double)pageSize)
            : (res.Items.Count > 0 ? page : 0);

        var i = position;
        dto.Items = res.Items.Select(r => new HikDeviceEventRow
        {
            No = ++i,
            EmployeeId = string.IsNullOrWhiteSpace(r.EmployeeNo) ? "--" : r.EmployeeNo!,
            Name = string.IsNullOrWhiteSpace(r.Name) ? "-" : r.Name!,
            CardNo = string.IsNullOrWhiteSpace(r.CardNo) ? "--" : r.CardNo!,
            EventType = HikEventTypes.Label(r.Major, r.Minor),
            Time = r.Time?.ToString("dd.MM.yyyy HH:mm:ss") ?? "-",
            PhotoUrl = string.IsNullOrWhiteSpace(r.PictureUrl)
                ? null
                : Url.Action("Photo", "AccessEvents", new { deviceId, u = r.PictureUrl })
        }).ToList();

        return Json(dto);
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
