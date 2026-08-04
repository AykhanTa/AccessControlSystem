using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

/// <summary>
/// Cihaz keçid hadisələrini emal edir. Hər hadisə AccessEvent kimi saxlanır (audit/tarixçə);
/// icazə verilən (granted) hadisə ziyarətin statusunu irəli aparır:
///   Giriş cihazı: Planlaşdırılmış/Kart verilib → Binadadır; Binadadır → Mərtəbədə.
///   Çıxış cihazı: → Çıxıb (kart boşalır; cihazdan silmə background job ilə).
/// </summary>
public class VisitEventService : IVisitEventService
{
    private readonly IVisitRepository _visits;
    private readonly IEmployeeRepository _employees;
    private readonly IDeviceRepository _devices;
    private readonly IAccessEventRepository _events;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public VisitEventService(IVisitRepository visits, IEmployeeRepository employees, IDeviceRepository devices,
        IAccessEventRepository events, IUnitOfWork uow, ISystemLogWriter log)
    {
        _visits = visits;
        _employees = employees;
        _devices = devices;
        _events = events;
        _uow = uow;
        _log = log;
    }

    public async Task ProcessAsync(HikEventDto ev, CancellationToken ct = default)
    {
        // Şəxssiz (qapı sensoru) hadisələri nəzərə alma.
        if (string.IsNullOrWhiteSpace(ev.AccessNumber))
            return;

        var device = string.IsNullOrWhiteSpace(ev.DeviceIp)
            ? null : await _devices.GetByIpAsync(ev.DeviceIp!, ct);

        // MATCH-FIRST: əvvəl aktiv QONAQ, sonra aktiv İŞÇİ. Heç biri yoxsa — nəzərə alma
        // (backlog / cihazdakı bizə aid olmayan istifadəçilər DB-ni doldurmasın).
        var visit = await _visits.GetActiveByAccessNumberAsync(ev.AccessNumber!, ct);
        if (visit is not null)
        {
            await ProcessVisitAsync(ev, device, visit, ct);
            return;
        }

        var employee = await _employees.GetActiveByAccessNumberAsync(ev.AccessNumber!, ct);
        if (employee is not null)
        {
            await ProcessEmployeeAsync(ev, device, employee, ct);
        }
    }

    private async Task ProcessVisitAsync(HikEventDto ev, Domain.Entities.Device? device, Visit visit, CancellationToken ct)
    {
        await _events.AddAsync(new AccessEvent
        {
            VisitId = visit.Id,
            DeviceId = device?.Id,
            AccessNumber = ev.AccessNumber!,
            PersonName = ev.PersonName ?? visit.Guest?.FullName,
            EventType = ev.Granted ? "AccessGranted" : "AccessDenied",
            Granted = ev.Granted,
            OccurredAt = ev.OccurredAt,
            DeviceIp = ev.DeviceIp,
            Raw = ev.Raw is { Length: > 4000 } ? ev.Raw[..4000] : ev.Raw
        }, ct);

        if (device is not null && ev.Granted)
        {
            var direction = device.AccessPoint?.Direction ?? device.Direction;
            var pointType = device.AccessPoint?.PointType;
            var newStatus = NextStatus(visit.Status, direction, pointType);
            if (newStatus != visit.Status)
            {
                visit.Status = newStatus;
                visit.UpdatedAt = DateTime.Now;
                // İlk dəfə binaya daxil olma anı (faktiki giriş vaxtı) — planlaşdırılan gəlişdən fərqli.
                if (visit.ActualEntryAt is null && newStatus is VisitStatus.In or VisitStatus.OnFloor)
                    visit.ActualEntryAt = DateTime.Now;
                if (newStatus == VisitStatus.Out)
                {
                    visit.ActualExitAt = DateTime.Now;
                    if (visit.Card is not null) { visit.Card.Status = CardStatus.Free; visit.Card.UpdatedAt = DateTime.Now; }
                }
                var dir = direction == DeviceDirection.Entry ? "giriş" : "çıxış";
                await _log.LogAsync("ACCESS_EVENT",
                    $"{visit.Guest?.FullName} — {device.AccessPoint?.Name ?? device.Floor?.Name} {dir} → {StatusLabel(newStatus)}.",
                    "visit", visit.Id, ct: ct);
            }
        }
        await _uow.SaveChangesAsync(ct);
    }

    private async Task ProcessEmployeeAsync(HikEventDto ev, Domain.Entities.Device? device, Employee emp, CancellationToken ct)
    {
        await _events.AddAsync(new AccessEvent
        {
            EmployeeId = emp.Id,
            DeviceId = device?.Id,
            AccessNumber = ev.AccessNumber!,
            PersonName = ev.PersonName ?? emp.FullName,
            EventType = ev.Granted ? "AccessGranted" : "AccessDenied",
            Granted = ev.Granted,
            OccurredAt = ev.OccurredAt,
            DeviceIp = ev.DeviceIp,
            Raw = ev.Raw is { Length: > 4000 } ? ev.Raw[..4000] : ev.Raw
        }, ct);

        // QEYD: işçi presence-i ARTIQ burada yenilənmir — DeviceEventPoller.ReconcilePresenceAsync
        // (bugün-əsaslı, ad-dəqiq uyğunlaşma) yeganə mənbədir. Burada yalnız AccessEvent (tarixçə) yazılır.
        await _uow.SaveChangesAsync(ct);
    }

    private static PresenceStatus NextPresence(PresenceStatus current, DeviceDirection dir, PointType? pointType)
    {
        if (dir == DeviceDirection.Exit || pointType is PointType.MainExit or PointType.FloorExit)
            return PresenceStatus.Out;
        if (pointType is PointType.FloorEntrance)
            return PresenceStatus.OnFloor;
        if (pointType is PointType.MainEntrance or PointType.Turnstile)
            return PresenceStatus.In;
        return current switch
        {
            PresenceStatus.Out => PresenceStatus.In,
            PresenceStatus.In => PresenceStatus.OnFloor,
            _ => current
        };
    }

    private static string PresenceLabel(PresenceStatus s) => s switch
    {
        PresenceStatus.In => "Binadadır",
        PresenceStatus.OnFloor => "Mərtəbədə",
        _ => "Çıxıb"
    };

    private static VisitStatus NextStatus(VisitStatus current, DeviceDirection dir, PointType? pointType)
    {
        // Çıxış (istiqamət və ya çıxış tipli nöqtə) → Çıxıb
        if (dir == DeviceDirection.Exit || pointType is PointType.MainExit or PointType.FloorExit)
            return VisitStatus.Out;

        // Giriş — keçid nöqtəsinin tipinə görə dəqiq status
        if (pointType is PointType.FloorEntrance)
            return VisitStatus.OnFloor;                                  // mərtəbəyə giriş → Mərtəbədə
        if (pointType is PointType.MainEntrance or PointType.Turnstile)
            return VisitStatus.In;                                       // binaya giriş → Binadadır

        // Fallback (Door / tip təyin edilməyib) — köhnə evristika
        return current switch
        {
            VisitStatus.Planned or VisitStatus.CheckedIn or VisitStatus.Late => VisitStatus.In,
            VisitStatus.In => VisitStatus.OnFloor,
            _ => current
        };
    }

    private static string StatusLabel(VisitStatus s) => s switch
    {
        VisitStatus.In => "Binadadır",
        VisitStatus.OnFloor => "Mərtəbədə",
        VisitStatus.Out => "Çıxıb",
        _ => s.ToString()
    };
}
