using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>
/// MÜVƏQQƏTİ test controller-i — real cihazda (appsettings "Hikvision:TestDevice")
/// ISAPI əməliyyatlarını əl ilə yoxlamaq üçün. Faza 3-də Device entity + biznes
/// axını hazır olanda SİLİNMƏLİDİR. Sadəlik üçün bütün action-lar GET-dir (brauzerdən).
///
/// Parolu koda/appsettings-ə yazma — user-secrets işlət:
///   dotnet user-secrets init
///   dotnet user-secrets set "Hikvision:TestDevice:Password" "REAL_PAROL"
/// </summary>
[Route("HikvisionTest")]
public class HikvisionTestController : Controller
{
    private readonly IHikvisionDeviceService _hik;
    private readonly IConfiguration _config;
    private readonly IVisitEventService _events;
    private readonly IAccessEventRepository _eventRepo;
    private readonly IDeviceRepository _devices;
    private readonly HikvisionOptions _opt;

    public HikvisionTestController(IHikvisionDeviceService hik, IConfiguration config,
        IVisitEventService events, IAccessEventRepository eventRepo,
        IDeviceRepository devices, HikvisionOptions opt)
    {
        _hik = hik;
        _config = config;
        _events = events;
        _eventRepo = eventRepo;
        _devices = devices;
        _opt = opt;
    }

    private HikDevice? Device(out string? error)
    {
        error = null;
        var s = _config.GetSection("Hikvision");
        var ip = s["TestDeviceIp"];
        var user = s["Username"];
        var pass = s["Password"];
        if (string.IsNullOrWhiteSpace(ip) || string.IsNullOrWhiteSpace(user))
        {
            error = "Hikvision konfiqurasiyası tam deyil (TestDeviceIp/Username).";
            return null;
        }
        if (string.IsNullOrWhiteSpace(pass))
        {
            error = "Cihaz parolu təyin edilməyib. user-secrets ilə qoyun: " +
                    "dotnet user-secrets set \"Hikvision:Password\" \"...\"";
            return null;
        }
        return new HikDevice(ip, user!, pass!,
            int.TryParse(s["Port"], out var p) ? p : 80,
            bool.TryParse(s["UseHttps"], out var h) && h);
    }

    private IActionResult Run(Func<HikDevice, Task<HikResult>> op)
    {
        var device = Device(out var error);
        if (device is null) return Json(new { ok = false, error });
        var result = op(device).GetAwaiter().GetResult();
        return Json(new
        {
            ok = result.Success,
            http = result.HttpStatus,
            statusCode = result.StatusCode,
            sub = result.SubStatusCode,
            error = result.ErrorMessage,
            raw = result.RawBody
        });
    }

    // GET /HikvisionTest/Ping — bağlantı + kredit yoxlaması
    [HttpGet("Ping")]
    public IActionResult Ping() => Run(d => _hik.TestConnectionAsync(d));

    // GET /HikvisionTest/GetTime
    [HttpGet("GetTime")]
    public IActionResult GetTime() => Run(d => _hik.GetTimeAsync(d));

    // GET /HikvisionTest/SetTime — cihaz saatını serverin indiki vaxtına qoyur
    [HttpGet("SetTime")]
    public IActionResult SetTime() => Run(d => _hik.SetTimeAsync(d, DateTime.Now));

    // GET /HikvisionTest/Enroll?no=2903913593&name=Test%20Qonaq&hours=24
    [HttpGet("Enroll")]
    public IActionResult Enroll(string no = "2903913593", string name = "Test Qonaq", int hours = 24)
    {
        var begin = DateTime.Now;
        var end = begin.AddHours(hours);
        return Run(d => _hik.EnrollAccessNumberAsync(d, no, name, begin, end));
    }

    // GET /HikvisionTest/Search?no=2903913593
    [HttpGet("Search")]
    public IActionResult Search(string no = "2903913593") => Run(d => _hik.SearchUserAsync(d, no));

    // GET /HikvisionTest/OpenDoor?door=1
    [HttpGet("OpenDoor")]
    public IActionResult OpenDoor(int door = 1) => Run(d => _hik.RemoteOpenDoorAsync(d, door));

    // GET /HikvisionTest/Revoke?no=2903913593 — kart + istifadəçini silir
    [HttpGet("Revoke")]
    public IActionResult Revoke(string no = "2903913593") => Run(d => _hik.RevokeAccessNumberAsync(d, no));

    // GET /HikvisionTest/ConfigureEvents?serverIp=10.130.0.50&serverPort=5082
    // Cihazı real-vaxt hadisələri bu serverə göndərməyə konfiqurasiya edir.
    // serverIp = bu kompüterin LAN IP-si (cihaz ora çata bilməlidir).
    [HttpGet("ConfigureEvents")]
    public IActionResult ConfigureEvents(string serverIp, int serverPort = 5082) =>
        Run(d => _hik.ConfigureEventHostAsync(d, serverIp, serverPort));

    // GET /HikvisionTest/EventHost — cihazdakı hazırkı httpHosts konfiqini göstərir (diaqnostika)
    [HttpGet("EventHost")]
    public IActionResult EventHost() => Run(d => _hik.GetEventHostAsync(d));

    // GET /HikvisionTest/FaceLibs — cihazdakı üz kitabxanalarını göstərir (FDID/faceLibType üçün)
    [HttpGet("FaceLibs")]
    public IActionResult FaceLibs() => Run(d => _hik.GetFaceLibsAsync(d));

    // GET /HikvisionTest/EventStats — serverə çatan BÜTÜN POST-lar (saxlanmasa belə).
    // totalReceived artırsa cihaz hələ göndərir; recent-də deviceTime 2026 + 2903913593 = canlı oxutma.
    [HttpGet("EventStats")]
    public IActionResult EventStats() => Json(Services.HikEventMonitor.Snapshot());

    // GET /HikvisionTest/Reboot — cihazı yenidən başladır (gözləyən event növbəsini təmizləmək üçün).
    // DİQQƏT: cihaz ~1-2 dəqiqə işləməyəcək (production-da işçilər keçə bilməyəcək).
    [HttpGet("Reboot")]
    public IActionResult Reboot() => Run(d => _hik.RebootAsync(d));

    // GET /HikvisionTest/DeleteEventHost — httpHosts push-u söndürür (POLL yanaşmasına keçəndə
    // backlog selini dayandırmaq üçün). Status artıq arxa-plan poller ilə yenilənir.
    [HttpGet("DeleteEventHost")]
    public IActionResult DeleteEventHost() => Run(d => _hik.DeleteEventHostAsync(d));

    // GET /HikvisionTest/SearchRecent?minutes=5 — cihazdan son N dəqiqənin icazə verilmiş oxutmaları (poll testi).
    [HttpGet("SearchRecent")]
    public async Task<IActionResult> SearchRecent(int minutes = 5)
    {
        var device = Device(out var error);
        if (device is null) return Json(new { ok = false, error });
        var deviceNow = await _hik.GetDeviceTimeAsync(device) ?? DateTimeOffset.Now;
        var list = await _hik.SearchRecentEventsAsync(device, deviceNow.AddMinutes(-minutes), deviceNow);
        return Json(new { ok = true, count = list.Count, events = list });
    }

    // GET /HikvisionTest/PollTest?minutes=10 — DB-dəki BÜTÜN aktiv cihazları yoxlayır:
    // ping (auth), cihaz saatı, son N dəqiqənin icazəli oxutmaları. Hansı cihazın niyə işləmədiyini göstərir.
    [HttpGet("PollTest")]
    public async Task<IActionResult> PollTest(int minutes = 10)
    {
        var devices = await _devices.GetAllActiveAsync();
        var results = new List<object>();
        foreach (var d in devices)
        {
            var target = new HikDevice(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);
            var ping = await _hik.TestConnectionAsync(target);
            var time = await _hik.GetTimeAsync(target);
            var deviceNow = await _hik.GetDeviceTimeAsync(target) ?? DateTimeOffset.Now;
            var list = await _hik.SearchRecentEventsAsync(target, deviceNow.AddMinutes(-minutes), deviceNow);
            results.Add(new
            {
                d.Id,
                d.Name,
                d.Ip,
                d.Port,
                direction = d.Direction.ToString(),
                pingOk = ping.Success,
                pingHttp = ping.HttpStatus,
                deviceTimeRaw = time.RawBody,
                grantedCount = list.Count,
                events = list.Select(e => new { e.AccessNumber, e.PersonName, e.SerialNo, e.MinorType })
            });
        }
        return Json(results);
    }

    // GET /HikvisionTest/SyncTimeAll — DB-dəki BÜTÜN aktiv cihazları serverin vaxtına + Bakı (CST-4)
    // timezone-una köçürür. Timezone fərqlərini aradan qaldırır.
    [HttpGet("SyncTimeAll")]
    public async Task<IActionResult> SyncTimeAll()
    {
        var devices = await _devices.GetAllActiveAsync();
        var results = new List<object>();
        foreach (var d in devices)
        {
            var target = new HikDevice(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);
            var res = await _hik.SetTimeAsync(target, DateTime.Now);
            results.Add(new { d.Name, d.Ip, ok = res.Success, http = res.HttpStatus });
        }
        return Json(results);
    }

    // GET /HikvisionTest/AcsEventRaw?minutes=10&major=0&minor=0 — cihazın XAM AcsEvent cavabı (diaqnostika).
    [HttpGet("AcsEventRaw")]
    public async Task<IActionResult> AcsEventRaw(int minutes = 10, int major = 0, int minor = 0, int position = 0)
    {
        var device = Device(out var error);
        if (device is null) return Json(new { ok = false, error });
        var deviceNow = await _hik.GetDeviceTimeAsync(device) ?? DateTimeOffset.Now;
        var res = await _hik.SearchAcsEventRawAsync(device, deviceNow.AddMinutes(-minutes), deviceNow, major, minor, position);
        return Json(new { ok = res.Success, http = res.HttpStatus, raw = res.RawBody });
    }

    // GET /HikvisionTest/ResetEvents?serverIp=PC_IP&serverPort=5082
    // httpHosts-u silir sonra təzədən konfiqurasiya edir — gözləyən köhnə event yığınını sıfırlamağa cəhd.
    [HttpGet("ResetEvents")]
    public IActionResult ResetEvents(string serverIp, int serverPort = 5082)
    {
        var device = Device(out var error);
        if (device is null) return Json(new { ok = false, error });
        var del = _hik.DeleteEventHostAsync(device).GetAwaiter().GetResult();
        System.Threading.Thread.Sleep(1000);
        var cfg = _hik.ConfigureEventHostAsync(device, serverIp, serverPort).GetAwaiter().GetResult();
        return Json(new
        {
            ok = cfg.Success,
            delete = new { del.Success, del.HttpStatus, raw = del.RawBody },
            configure = new { cfg.Success, cfg.HttpStatus, raw = cfg.RawBody }
        });
    }

    // GET /HikvisionTest/Events — serverə ÇATAN son 20 hadisə (real cihazdan gəldiyini yoxlamaq üçün)
    [HttpGet("Events")]
    public async Task<IActionResult> Events()
    {
        var list = await _eventRepo.GetRecentAsync(20);
        return Json(list.Select(e => new
        {
            e.Id,
            time = e.OccurredAt,
            e.AccessNumber,
            e.PersonName,
            e.EventType,
            e.Granted,
            e.DeviceIp,
            device = e.Device?.Name,
            matchedVisit = e.VisitId,
            raw = e.Raw
        }));
    }

    // GET /HikvisionTest/SimulateEvent?no=2903913593 — cihazı gözləmədən bir "oxutma" simulyasiya edir.
    // Pipeline + status keçidini (Qonaqlar səhifəsində canlı) yoxlamaq üçün.
    [HttpGet("SimulateEvent")]
    public async Task<IActionResult> SimulateEvent(string no = "2903913593", string? ip = null)
    {
        var deviceIp = ip ?? _config["Hikvision:TestDeviceIp"] ?? "10.130.0.189";
        await _events.ProcessAsync(new HikEventDto
        {
            AccessNumber = no,
            PersonName = "(simulyasiya)",
            DeviceIp = deviceIp,
            MajorType = 5,
            MinorType = 1,
            Granted = true,
            OccurredAt = DateTime.Now,
            Raw = "{\"simulated\":true}"
        });
        return Json(new
        {
            ok = true,
            note = $"{deviceIp} cihazından {no} oxutması emal edildi. Qonaqlar səhifəsində status ~4 saniyəyə dəyişməlidir."
        });
    }
}
