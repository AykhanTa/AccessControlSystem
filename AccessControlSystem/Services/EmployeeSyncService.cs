using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Services;

/// <summary>
/// İşçini icazəli mərtəbələrin cihazlarına yazır (UserInfo) və üzünü yükləyir (FaceDataRecord).
/// Vahid nömrə: employeeNo = cihazdakı employeeNo = AccessNumber (üz/kart eyni nömrəyə bağlıdır).
/// </summary>
public class EmployeeSyncService
{
    private readonly IEmployeeRepository _employees;
    private readonly IDeviceRepository _devices;
    private readonly IHikvisionDeviceService _hik;
    private readonly HikvisionOptions _opt;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;
    private readonly IWebHostEnvironment _env;

    public EmployeeSyncService(IEmployeeRepository employees, IDeviceRepository devices,
        IHikvisionDeviceService hik, HikvisionOptions opt, IUnitOfWork uow, ISystemLogWriter log,
        IWebHostEnvironment env)
    {
        _employees = employees;
        _devices = devices;
        _hik = hik;
        _opt = opt;
        _uow = uow;
        _log = log;
        _env = env;
    }

    private HikDevice Target(Domain.Entities.Device d) =>
        new(d.Ip, _opt.Username, _opt.Password, d.Port == 0 ? _opt.Port : d.Port, d.UseHttps);

    public async Task<string> SyncAsync(long employeeId, CancellationToken ct = default)
    {
        var emp = await _employees.GetByIdAsync(employeeId, ct)
                  ?? throw new KeyNotFoundException("İşçi tapılmadı.");

        var floorIds = emp.EmployeeFloors.Select(f => f.FloorId).ToList();
        if (floorIds.Count == 0)
            return "İşçiyə mərtəbə təyin edilməyib — cihaza yazılmadı.";

        var devices = await _devices.GetActiveByFloorIdsAsync(floorIds, ct);
        if (devices.Count == 0)
            return "İcazəli mərtəbələrdə aktiv cihaz yoxdur.";

        // Cihaz üçün NUMERİK nömrə (Hikvision employeeNo tire/hərfi rədd edə bilər).
        // Sabit: "9" + Id — real işçilərlə (1-100) toqquşmasın. Bir dəfə təyin olunur.
        var number = emp.AccessNumber;
        if (string.IsNullOrWhiteSpace(number) || !number.All(char.IsDigit))
            number = $"9{emp.Id:D7}";
        emp.AccessNumber = number;

        var begin = emp.EmploymentStartAt ?? DateTime.Now;
        var end = emp.EmploymentEndAt ?? DateTime.Now.AddYears(10);
        var photo = ReadPhoto(emp.PhotoPath);

        int userOk = 0, faceOk = 0, faceTried = 0;
        string? faceError = null, userError = null;
        foreach (var d in devices)
        {
            var target = Target(d);
            var user = new HikUser(number, emp.FullName, begin, end);

            var res = await _hik.CreateUserAsync(target, user, ct);
            if (!res.Success) res = await _hik.UpdateUserAsync(target, user, ct);
            if (res.Success) userOk++;
            else userError ??= $"HTTP {res.HttpStatus}: {res.RawBody}";

            if (photo is not null)
            {
                faceTried++;
                var f = await _hik.UploadFaceAsync(target, number, photo, ct);
                // "deviceUserAlreadyExistFace" = üz artıq cihazdadır → uğur sayılır (idempotent).
                if (f.Success || f.SubStatusCode == "deviceUserAlreadyExistFace") faceOk++;
                else faceError ??= $"HTTP {f.HttpStatus}: {f.RawBody}";
            }
        }

        emp.FaceStatus = photo is null
            ? FaceStatus.None
            : (faceTried > 0 && faceOk == faceTried ? FaceStatus.Synced : FaceStatus.Failed);
        emp.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);

        await _log.LogAsync("EMPLOYEE_SYNC",
            $"{emp.FullName} ({number}) — {userOk}/{devices.Count} cihaza yazıldı, üz {faceOk}/{faceTried}.",
            "employee", emp.Id, ct: ct);

        var msg = $"{userOk}/{devices.Count} cihaza yazıldı" +
                  (photo is null ? " (foto yoxdur, üz yüklənmədi)." : $", üz {faceOk}/{faceTried} cihaza yükləndi.");
        if (userError is not null)
            msg += $" İSTİFADƏÇİ XƏTASI → {userError}";
        if (faceError is not null)
            msg += $" ÜZ XƏTASI → {faceError}";
        return msg;
    }

    private byte[]? ReadPhoto(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath)) return null;
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var rel = photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(webRoot, rel);
        return File.Exists(full) ? File.ReadAllBytes(full) : null;
    }
}
