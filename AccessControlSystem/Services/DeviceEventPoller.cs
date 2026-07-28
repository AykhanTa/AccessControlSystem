using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;

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

        // Baseline-ı DƏRHAL qoy (istifadəçi oxutmasından əvvəlki cari vəziyyət) —
        // beləliklə app başladıqdan sonrakı İLK oxutma da emal olunur.
        await InitBaselinesAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try { await PollAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Cihaz event poll xətası"); }
            try { await Task.Delay(Interval, ct); } catch (TaskCanceledException) { break; }
        }
    }

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
