using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

/// <summary>
/// Ziyarəti cihazlara yazan/silən orchestration. Hər cihaz üçün ayrıca ISAPI
/// çağırışı edir və nəticəni DeviceEnrollment-də saxlayır. Bir cihaz uğursuz olsa
/// belə digərləri davam edir — qonaq qeydiyyatı bloklanmır (Failed olanlar sonradan
/// retry job ilə təkrar cəhd oluna bilər).
/// </summary>
public class VisitAccessService : IVisitAccessService
{
    private readonly IDeviceRepository _devices;
    private readonly IDeviceEnrollmentRepository _enrollments;
    private readonly IHikvisionDeviceService _hik;
    private readonly IUnitOfWork _uow;
    private readonly HikvisionOptions _opt;
    private readonly ISystemLogWriter _log;

    public VisitAccessService(
        IDeviceRepository devices, IDeviceEnrollmentRepository enrollments,
        IHikvisionDeviceService hik, IUnitOfWork uow, HikvisionOptions opt, ISystemLogWriter log)
    {
        _devices = devices;
        _enrollments = enrollments;
        _hik = hik;
        _uow = uow;
        _opt = opt;
        _log = log;
    }

    private HikDevice Target(Device d) =>
        new(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);

    public async Task<int> EnrollAsync(long visitId, string accessNumber, string guestName,
        DateTime begin, DateTime end, IEnumerable<long> floorIds, CancellationToken ct = default)
    {
        var devices = await _devices.GetActiveByFloorIdsAsync(floorIds, ct);
        if (devices.Count == 0) return 0;

        var synced = 0;
        foreach (var device in devices)
        {
            var enrollment = new DeviceEnrollment
            {
                VisitId = visitId,
                DeviceId = device.Id,
                AccessNumber = accessNumber,
                Attempts = 1
            };
            try
            {
                var res = await _hik.EnrollAccessNumberAsync(Target(device), accessNumber, guestName, begin, end, ct);
                if (res.Success)
                {
                    enrollment.Status = EnrollmentStatus.Synced;
                    enrollment.SyncedAt = DateTime.Now;
                    synced++;
                }
                else
                {
                    enrollment.Status = EnrollmentStatus.Failed;
                    enrollment.LastError = Trim(res.ToString());
                }
            }
            catch (Exception ex)
            {
                enrollment.Status = EnrollmentStatus.Failed;
                enrollment.LastError = Trim(ex.Message);
            }
            await _enrollments.AddAsync(enrollment, ct);
        }

        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("DEVICE_ENROLL",
            $"{guestName} ({accessNumber}) {synced}/{devices.Count} cihaza yazıldı.",
            "visit", visitId, ct: ct);
        return synced;
    }

    public async Task RevokeAsync(long visitId, CancellationToken ct = default)
    {
        var enrollments = await _enrollments.GetByVisitAsync(visitId, ct);
        if (enrollments.Count == 0) return;

        foreach (var e in enrollments)
        {
            if (e.Device is null) continue;
            try
            {
                await _hik.RevokeAccessNumberAsync(Target(e.Device), e.AccessNumber, ct);
                e.Status = EnrollmentStatus.Revoked;
                e.LastError = null;
            }
            catch (Exception ex)
            {
                e.LastError = Trim(ex.Message);
            }
            e.UpdatedAt = DateTime.Now;
        }

        await _uow.SaveChangesAsync(ct);
        var guestName = enrollments[0].Visit?.Guest?.FullName ?? $"Ziyarət #{visitId}";
        await _log.LogAsync("DEVICE_REVOKE",
            $"{guestName} üçün {enrollments.Count} cihaz-qeydi silindi.", "visit", visitId, ct: ct);
    }

    private static string Trim(string s) => s.Length > 500 ? s[..500] : s;
}
