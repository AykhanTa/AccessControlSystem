using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Services;

/// <summary>
/// Arxa-plan təmizləmə: gecikən qonaqları işarələyir, çıxmış/vaxtı keçmiş qonaqları
/// grace period sonra cihazlardan silir (kart təkrar istifadə qorunması ilə) və kartı boşaldır.
/// Cihaz yaddaşının (3000 istifadəçi) dolmasının qarşısını alır.
/// </summary>
public class VisitMaintenanceService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<VisitMaintenanceService> _logger;

    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan LateAfter = TimeSpan.FromMinutes(30);   // gözlənilən çıxışdan sonra → Gecikib
    private static readonly TimeSpan Grace = TimeSpan.FromMinutes(10);       // çıxışdan sonra cihazdan silmə gözləmə
    private static readonly TimeSpan HardExpiry = TimeSpan.FromHours(12);    // bu qədər keçibsə məcburi çıxış + təmizlə

    public VisitMaintenanceService(IServiceScopeFactory scopes, ILogger<VisitMaintenanceService> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        try { await Task.Delay(TimeSpan.FromSeconds(30), ct); } catch (TaskCanceledException) { return; }
        while (!ct.IsCancellationRequested)
        {
            try { await RunAsync(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "Ziyarət təmizləmə xətası"); }
            try { await Task.Delay(Interval, ct); } catch (TaskCanceledException) { break; }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var visits = scope.ServiceProvider.GetRequiredService<IVisitRepository>();
        var access = scope.ServiceProvider.GetRequiredService<IVisitAccessService>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var log = scope.ServiceProvider.GetRequiredService<ISystemLogWriter>();

        var now = DateTime.Now;
        var candidates = await visits.GetForMaintenanceAsync(ct);
        var toClean = new List<Visit>();

        foreach (var v in candidates)
        {
            var inBuilding = v.Status is VisitStatus.CheckedIn or VisitStatus.In or VisitStatus.OnFloor;

            // Sərt vaxt bitmə → məcburi çıxış (+ təmizlə)
            if (inBuilding && v.ExpectedExitAt is { } hardExp && now > hardExp + HardExpiry)
            {
                v.Status = VisitStatus.Out;
                v.ActualExitAt = now;
                v.UpdatedAt = now;
                await log.LogAsync("VISIT_EXPIRED", $"{v.Guest?.FullName} — vaxtı bitdi, avtomatik çıxış.", "visit", v.Id, ct: ct);
                toClean.Add(v);
                continue;
            }

            // Gecikmə işarəsi
            if (inBuilding && v.Status != VisitStatus.Late && v.ExpectedExitAt is { } exp && now > exp + LateAfter)
            {
                v.Status = VisitStatus.Late;
                v.UpdatedAt = now;
                await log.LogAsync("VISIT_LATE", $"{v.Guest?.FullName} gecikdi (gözlənilən çıxış keçdi).", "visit", v.Id, ct: ct);
            }

            // Təmizləmə: gecikmiş (dərhal) VƏ YA çıxmış (grace period sonra) — cihazda hələ qeydi varsa.
            var exitAt = v.ActualExitAt ?? v.UpdatedAt;
            var hasLiveEnrollment = v.DeviceEnrollments.Any(e => e.Status != EnrollmentStatus.Revoked);
            if (hasLiveEnrollment && (
                    v.Status == VisitStatus.Late ||
                    (v.Status == VisitStatus.Out && exitAt + Grace < now)))
            {
                toClean.Add(v);
            }
        }

        await uow.SaveChangesAsync(ct);

        foreach (var v in toClean)
        {
            var reused = !string.IsNullOrEmpty(v.AccessNumber)
                         && await visits.HasOtherActiveWithAccessNumberAsync(v.AccessNumber, v.Id, ct);
            if (reused)
            {
                // Başqa aktiv ziyarət eyni nömrəni istifadə edir — cihazdan SİLMƏ (onu pozar),
                // sadəcə bu ziyarətin qeydlərini məntiqi olaraq bağla; kartı da toxunma.
                foreach (var e in v.DeviceEnrollments) { e.Status = EnrollmentStatus.Revoked; e.UpdatedAt = now; }
                continue;
            }

            await access.RevokeAsync(v.Id, ct);   // cihazlardan sil + enrollment Revoked

            // Kartı boşalt (təkrar istifadə üçün)
            if (v.Card is not null && v.Card.Status != CardStatus.Free)
            {
                v.Card.Status = CardStatus.Free;
                v.Card.UpdatedAt = now;
            }
        }

        await uow.SaveChangesAsync(ct);
    }
}
