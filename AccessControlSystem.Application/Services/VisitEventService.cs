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
    private readonly IDeviceRepository _devices;
    private readonly IAccessEventRepository _events;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public VisitEventService(IVisitRepository visits, IDeviceRepository devices,
        IAccessEventRepository events, IUnitOfWork uow, ISystemLogWriter log)
    {
        _visits = visits;
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

        // MATCH-FIRST: yalnız bizim aktiv qonağa uyğun gələn hadisəni saxla/emal et.
        // Backlog və cihazdakı başqa istifadəçilər (uyğun ziyarət yoxdur) tez keçilir —
        // bu, cihaz saatından asılı deyil və DB-ni doldurmur.
        var visit = await _visits.GetActiveByAccessNumberAsync(ev.AccessNumber!, ct);
        if (visit is null)
            return;

        var device = string.IsNullOrWhiteSpace(ev.DeviceIp)
            ? null : await _devices.GetByIpAsync(ev.DeviceIp!, ct);

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

        // Status keçidi yalnız tanınan cihaz + icazə verildikdə.
        if (device is not null && ev.Granted)
        {
            var newStatus = NextStatus(visit.Status, device.Direction);
            if (newStatus != visit.Status)
            {
                visit.Status = newStatus;
                visit.UpdatedAt = DateTime.Now;

                if (newStatus == VisitStatus.Out)
                {
                    visit.ActualExitAt = DateTime.Now;
                    if (visit.Card is not null)
                    {
                        visit.Card.Status = CardStatus.Free;
                        visit.Card.UpdatedAt = DateTime.Now;
                    }
                }

                var dir = device.Direction == DeviceDirection.Entry ? "giriş" : "çıxış";
                await _log.LogAsync("ACCESS_EVENT",
                    $"{visit.Guest?.FullName} — {device.Floor?.Name} {dir} → {StatusLabel(newStatus)}.",
                    "visit", visit.Id, ct: ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
    }

    private static VisitStatus NextStatus(VisitStatus current, DeviceDirection dir)
    {
        if (dir == DeviceDirection.Exit)
            return VisitStatus.Out;

        // Giriş cihazı
        return current switch
        {
            VisitStatus.Planned or VisitStatus.CheckedIn or VisitStatus.Late => VisitStatus.In,  // binaya giriş
            VisitStatus.In => VisitStatus.OnFloor,   // mərtəbəyə giriş
            _ => current                              // OnFloor → OnFloor
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
