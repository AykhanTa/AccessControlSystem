using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Services;

/// <summary>
/// Cihazların hadisə jurnalını PULL edir (hər N saniyədə son aralığı sorğular).
/// httpHosts push-un backlog problemini keçir — yalnız təzə (vaxt pəncərəsindəki) icazə
/// verilmiş oxutmaları götürür və status keçidini icra edir. serialNo ilə təkrar emalın qarşısı alınır.
/// Baseline app başlayanda dərhal qoyulur ki, ilk real oxutma da tutulsun.
/// </summary>
public class DeviceEventPoller : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly HikvisionOptions _opt;
    private readonly ILogger<DeviceEventPoller> _logger;
    private readonly Dictionary<long, int> _lastSerial = new();   // deviceId → sonuncu emal olunan serialNo

    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(60);

    public DeviceEventPoller(IServiceScopeFactory scopes, HikvisionOptions opt, ILogger<DeviceEventPoller> logger)
    {
        _scopes = scopes;
        _opt = opt;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(3), ct); } catch (TaskCanceledException) { return; }

        await InitBaselinesAsync(ct);
        // Başlanğıcda presence-i BUGÜNKÜ keçidlərdən qur (bugün skan yoxdursa → Çıxış etdi).
        try { await ReconcilePresenceAsync(ct); }
        catch (Exception ex) { _logger.LogWarning(ex, "Presence reconcile (start) xətası"); }

        var tick = 0;
        while (!ct.IsCancellationRequested)
        {
            try { await PollAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Cihaz event poll xətası"); }

            // Hər ~20 saniyədə presence-i BUGÜNKÜ keçidlərə görə tam uzlaşdır (yeganə mənbə — dəqiq).
            if (++tick % 4 == 0)
            {
                try { await ReconcilePresenceAsync(ct); }
                catch (Exception ex) { _logger.LogWarning(ex, "Presence reconcile xətası"); }
            }

            try { await Task.Delay(Interval, ct); } catch (TaskCanceledException) { break; }
        }
    }

    private const int ReconcilePages = 40;   // son 3 günün hadisələrini əhatə etmək üçün səhifə limiti

    private HikDevice Target(Device d) =>
        new(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);

    private async Task InitBaselinesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopes.CreateScope();
            var devices = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
            var hik = scope.ServiceProvider.GetRequiredService<IHikvisionDeviceService>();

            foreach (var d in await devices.GetAllActiveAsync(ct))
            {
                var target = Target(d);
                // Cihazın ÖZ saatı ilə sorğula (timezone fərqi olsa belə işləsin).
                var deviceNow = await hik.GetDeviceTimeAsync(target, ct) ?? DateTimeOffset.Now;
                var list = await hik.SearchRecentEventsAsync(target, deviceNow.AddMinutes(-10), deviceNow, ct);
                _lastSerial[d.Id] = list.Count > 0 ? list.Max(e => e.SerialNo ?? 0) : 0;
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Baseline init xətası"); }
    }

    /// <summary>
    /// Presence-i cihazların FAKTİKİ son hadisələrindən yenidən qurur (self-healing).
    /// Presence-i YALNIZ BUGÜNKÜ keçidlərdən hesablayır (yeganə mənbə): bugün son hadisə girişdirsə
    /// Binadadır/Mərtəbədə; bugün skan yoxdursa → Çıxış etdi (gündəlik avtomatik sıfırlanma). Ad-əsaslı
    /// dəqiq uyğunlaşma (EmployeeMatcher). AccessEvent YAZMIR — yalnız presence.
    /// </summary>
    private async Task ReconcilePresenceAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var deviceRepo = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var empRepo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var hik = scope.ServiceProvider.GetRequiredService<IHikvisionDeviceService>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var devices = (await deviceRepo.GetAllWithFloorAsync(ct)).Where(d => d.IsActive).ToList();
        if (devices.Count == 0) return;

        var employees = await empRepo.GetAllAsync(ct);
        var matcher = new EmployeeMatcher(employees);

        // Son 3 günün hadisələrini topla (LastSeen üçün dəqiq son keçid). isToday = hadisə bugündürmü.
        var events = new List<(Device dev, string number, string? name, DateTimeOffset time, TimeSpan age, bool isToday)>();

        foreach (var d in devices)
        {
            var target = Target(d);
            var deviceNow = await hik.GetDeviceTimeAsync(target, ct) ?? DateTimeOffset.Now;
            var from = deviceNow.AddDays(-3);
            var today = deviceNow.Date;
            var position = 0;

            for (var page = 0; page < ReconcilePages; page++)   // ən yeni əvvəl (timeReverseOrder)
            {
                HikEventRawPage res;
                try { res = await hik.SearchEventPageAsync(target, from, deviceNow, position, 100, ct: ct); }
                catch { break; }
                if (!res.Ok || res.Items.Count == 0) break;

                foreach (var e in res.Items)
                {
                    if (string.IsNullOrWhiteSpace(e.EmployeeNo)) continue;   // yalnız şəxsli auth
                    if (e.Minor is not (1 or 75)) continue;                 // kart/QR = 1, üz = 75
                    if (e.Time is not { } t) continue;
                    var age = deviceNow - t;
                    if (age < TimeSpan.Zero) age = TimeSpan.Zero;
                    events.Add((d, e.EmployeeNo!.Trim(), e.Name, t, age, t.Date == today));
                }

                position += res.Items.Count;
                if (res.Status != "MORE") break;
            }
        }

        // Hər işçinin FAKTİKİ son keçidini (ən kiçik yaş) tap.
        var latest = new Dictionary<long, (TimeSpan age, Device dev, DateTimeOffset time, bool isToday)>();
        foreach (var ev in events)
        {
            var empId = matcher.Resolve(ev.dev.Id, ev.number, ev.name);
            if (empId is null) continue;
            if (!latest.TryGetValue(empId.Value, out var cur) || ev.age < cur.age)
                latest[empId.Value] = (ev.age, ev.dev, ev.time, ev.isToday);
        }

        // Presence: son keçid BUGÜNdürsə statusa görə; deyilsə → Çıxış etdi (gündəlik sıfırlanma).
        // LastSeen: faktiki son keçid vaxtı (dünənki ola bilər).
        var changed = false;
        foreach (var emp in employees)
        {
            if (latest.TryGetValue(emp.Id, out var val))
            {
                var presence = val.isToday ? AbsolutePresence(val.dev) : PresenceStatus.Out;
                var seenAt = val.time.LocalDateTime;
                if (emp.CurrentPresence != presence) { emp.CurrentPresence = presence; changed = true; }
                if (emp.LastSeenAt is null || emp.LastSeenAt < seenAt) { emp.LastSeenAt = seenAt; changed = true; }
            }
            else if (emp.CurrentPresence != PresenceStatus.Out)
            {
                emp.CurrentPresence = PresenceStatus.Out;   // son 3 gündə keçid yox → Çıxış etdi
                changed = true;
            }
        }

        if (changed) await uow.SaveChangesAsync(ct);
    }

    /// <summary>Bir keçid nöqtəsinin nəticə etdiyi MÜTLƏQ mövqe (tək son hadisədən presence hesablamaq üçün).</summary>
    private static PresenceStatus AbsolutePresence(Device d)
    {
        var dir = d.AccessPoint?.Direction ?? d.Direction;
        if (dir == DeviceDirection.Exit) return PresenceStatus.Out;
        return d.AccessPoint?.PointType == PointType.FloorEntrance
            ? PresenceStatus.OnFloor
            : PresenceStatus.In;
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var devices = scope.ServiceProvider.GetRequiredService<IDeviceRepository>();
        var hik = scope.ServiceProvider.GetRequiredService<IHikvisionDeviceService>();
        var events = scope.ServiceProvider.GetRequiredService<IVisitEventService>();

        foreach (var d in await devices.GetAllActiveAsync(ct))
        {
            var target = Target(d);
            // Cihazın ÖZ saatı ilə sorğula — server ilə cihazın timezone-u fərqli ola bilər.
            var deviceNow = await hik.GetDeviceTimeAsync(target, ct) ?? DateTimeOffset.Now;
            var list = await hik.SearchRecentEventsAsync(target, deviceNow - Window, deviceNow, ct);
            if (list.Count == 0) continue;

            var last = _lastSerial.TryGetValue(d.Id, out var v) ? v : 0;
            var maxSerial = last;

            foreach (var ev in list.Where(e => (e.SerialNo ?? 0) > last).OrderBy(e => e.SerialNo ?? 0))
            {
                await events.ProcessAsync(ev, ct);
                if ((ev.SerialNo ?? 0) > maxSerial) maxSerial = ev.SerialNo!.Value;
            }
            _lastSerial[d.Id] = maxSerial;
        }
    }
}
