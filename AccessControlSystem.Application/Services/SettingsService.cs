using System.Text.RegularExpressions;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class SettingsService : ISettingsService
{
    private readonly IHostRepository _hosts;
    private readonly IAreaRepository _areas;
    private readonly IPurposeRepository _purposes;
    private readonly IFloorRepository _floors;
    private readonly IDeviceRepository _devices;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public SettingsService(IHostRepository hosts, IAreaRepository areas, IPurposeRepository purposes,
                           IFloorRepository floors, IDeviceRepository devices,
                           IUnitOfWork uow, ISystemLogWriter log)
    {
        _hosts = hosts;
        _areas = areas;
        _purposes = purposes;
        _floors = floors;
        _devices = devices;
        _uow = uow;
        _log = log;
    }

    // ---------- Qəbul edən şəxslər ----------

    public async Task<List<HostItemDto>> GetHostsAsync(CancellationToken ct = default) =>
        (await _hosts.GetAllAsync(ct)).Select(h => new HostItemDto
        {
            Id = h.Id, FirstName = h.FirstName, LastName = h.LastName,
            Email = h.Email, Phone = h.Phone, Department = h.Department, IsActive = h.IsActive
        }).ToList();

    public async Task<long> AddHostAsync(HostInputDto dto, CancellationToken ct = default)
    {
        ValidateHost(dto);
        var host = new Host
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email?.Trim(),
            Phone = dto.Phone?.Trim(),
            Department = dto.Department?.Trim(),
            IsActive = true
        };
        await _hosts.AddAsync(host, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("HOST_CREATED", $"{host.FullName} qəbul edən şəxsi əlavə edildi.", "host", host.Id, ct: ct);
        return host.Id;
    }

    public async Task UpdateHostAsync(long id, HostInputDto dto, CancellationToken ct = default)
    {
        var host = await _hosts.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Qəbul edən tapılmadı.");
        ValidateHost(dto);
        host.FirstName = dto.FirstName.Trim();
        host.LastName = dto.LastName.Trim();
        host.Email = dto.Email?.Trim();
        host.Phone = dto.Phone?.Trim();
        host.Department = dto.Department?.Trim();
        host.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("HOST_UPDATED", $"{host.FullName} qəbul edən şəxsi yeniləndi.", "host", host.Id, ct: ct);
    }

    public async Task ToggleHostAsync(long id, CancellationToken ct = default)
    {
        var host = await _hosts.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Qəbul edən tapılmadı.");
        host.IsActive = !host.IsActive;
        host.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("HOST_STATUS_CHANGED",
            $"{host.FullName} qəbul edən şəxs {(host.IsActive ? "aktiv" : "deaktiv")} edildi.", "host", host.Id, ct: ct);
    }

    public async Task DeleteHostAsync(long id, CancellationToken ct = default)
    {
        var host = await _hosts.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Qəbul edən tapılmadı.");
        var name = host.FullName;
        try
        {
            _hosts.Remove(host);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Bu qəbul edən ziyarətlərdə istifadə olunub, silinə bilməz. Onu deaktiv edin.");
        }
        await _log.LogAsync("HOST_DELETED", $"{name} qəbul edən şəxsi silindi.", "host", id, ct: ct);
    }

    private static void ValidateHost(HostInputDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            throw new ArgumentException("Ad və Soyad daxil edin.");
        if (!string.IsNullOrWhiteSpace(dto.Email) && !Regex.IsMatch(dto.Email, @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            throw new ArgumentException("Düzgün email daxil edin.");
    }

    // ---------- Giriş əraziləri ----------

    public async Task<List<AreaItemDto>> GetAreasAsync(CancellationToken ct = default)
    {
        var areas = await _areas.GetAllAsync(ct);
        var result = new List<AreaItemDto>();
        foreach (var a in areas)
            result.Add(new AreaItemDto { Id = a.Id, Name = a.Name, UsageCount = await _areas.UsageCountAsync(a.Id, ct) });
        return result;
    }

    public async Task<long> AddAreaAsync(string name, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ərazi adını daxil edin.");
        if (await _areas.ExistsByNameAsync(name, ct))
            throw new ArgumentException("Bu ərazi artıq mövcuddur.");
        var area = new Area { Name = name, IsActive = true };
        await _areas.AddAsync(area, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LOCATION_CREATED", $"{area.Name} məkanı əlavə edildi.", "area", area.Id, ct: ct);
        return area.Id;
    }

    public async Task DeleteAreaAsync(long id, CancellationToken ct = default)
    {
        var area = await _areas.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Ərazi tapılmadı.");
        if (await _areas.UsageCountAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu ərazi ziyarətlərdə istifadə olunub, silinə bilməz.");
        var name = area.Name;
        _areas.Remove(area);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LOCATION_DELETED", $"{name} məkanı silindi.", "area", id, ct: ct);
    }

    // ---------- Gəliş məqsədləri ----------

    public async Task<List<PurposeItemDto>> GetPurposesAsync(CancellationToken ct = default) =>
        (await _purposes.GetAllAsync(ct)).Select(p => new PurposeItemDto
        {
            Id = p.Id, Name = p.Name, IsActive = p.IsActive
        }).ToList();

    public async Task<long> AddPurposeAsync(string name, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Məqsəd adını daxil edin.");
        if (await _purposes.ExistsByNameAsync(name, ct))
            throw new ArgumentException("Bu məqsəd artıq mövcuddur.");
        var purpose = new Purpose { Name = name, IsActive = true };
        await _purposes.AddAsync(purpose, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("PURPOSE_CREATED", $"{purpose.Name} məqsədi əlavə edildi.", "purpose", purpose.Id, ct: ct);
        return purpose.Id;
    }

    public async Task TogglePurposeAsync(long id, CancellationToken ct = default)
    {
        var purpose = await _purposes.GetByIdAsync(id, ct)
                      ?? throw new KeyNotFoundException("Məqsəd tapılmadı.");
        purpose.IsActive = !purpose.IsActive;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("PURPOSE_STATUS_CHANGED",
            $"{purpose.Name} məqsədi {(purpose.IsActive ? "aktiv" : "deaktiv")} edildi.", "purpose", purpose.Id, ct: ct);
    }

    // ---------- Mərtəbələr ----------

    public async Task<List<FloorItemDto>> GetFloorsAsync(CancellationToken ct = default)
    {
        var floors = await _floors.GetAllAsync(ct);
        var result = new List<FloorItemDto>();
        foreach (var f in floors)
            result.Add(new FloorItemDto
            {
                Id = f.Id, Name = f.Name, IsActive = f.IsActive,
                DeviceCount = await _floors.DeviceCountAsync(f.Id, ct)
            });
        return result;
    }

    public async Task<long> AddFloorAsync(string name, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mərtəbə adını daxil edin.");
        if (await _floors.ExistsByNameAsync(name, null, ct))
            throw new ArgumentException("Bu mərtəbə artıq mövcuddur.");
        var floor = new Floor { Name = name, IsActive = true };
        await _floors.AddAsync(floor, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("FLOOR_CREATED", $"{floor.Name} mərtəbəsi əlavə edildi.", "floor", floor.Id, ct: ct);
        return floor.Id;
    }

    public async Task ToggleFloorAsync(long id, CancellationToken ct = default)
    {
        var floor = await _floors.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("Mərtəbə tapılmadı.");
        floor.IsActive = !floor.IsActive;
        floor.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("FLOOR_STATUS_CHANGED",
            $"{floor.Name} mərtəbəsi {(floor.IsActive ? "aktiv" : "deaktiv")} edildi.", "floor", floor.Id, ct: ct);
    }

    public async Task DeleteFloorAsync(long id, CancellationToken ct = default)
    {
        var floor = await _floors.GetByIdAsync(id, ct)
                    ?? throw new KeyNotFoundException("Mərtəbə tapılmadı.");
        if (await _floors.DeviceCountAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu mərtəbədə cihazlar var. Əvvəlcə cihazları silin.");
        if (await _floors.VisitUsageCountAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu mərtəbə ziyarətlərdə istifadə olunub, silinə bilməz. Deaktiv edin.");
        var name = floor.Name;
        _floors.Remove(floor);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("FLOOR_DELETED", $"{name} mərtəbəsi silindi.", "floor", id, ct: ct);
    }

    // ---------- Cihazlar ----------

    public async Task<List<DeviceItemDto>> GetDevicesAsync(CancellationToken ct = default) =>
        (await _devices.GetAllWithFloorAsync(ct)).Select(d => new DeviceItemDto
        {
            Id = d.Id, Name = d.Name, Ip = d.Ip, Port = d.Port, UseHttps = d.UseHttps,
            DoorNo = d.DoorNo, FloorId = d.FloorId, FloorName = d.Floor?.Name ?? "",
            Direction = d.Direction.ToString(), IsActive = d.IsActive
        }).ToList();

    public async Task<long> AddDeviceAsync(DeviceInputDto dto, CancellationToken ct = default)
    {
        ValidateDevice(dto);
        if (await _devices.ExistsByIpPortAsync(dto.Ip.Trim(), dto.Port, null, ct))
            throw new ArgumentException("Bu IP:port ünvanı ilə cihaz artıq mövcuddur.");
        if (await _floors.GetByIdAsync(dto.FloorId, ct) is null)
            throw new ArgumentException("Mərtəbə tapılmadı.");

        var device = new Device
        {
            Name = dto.Name.Trim(),
            Ip = dto.Ip.Trim(),
            Port = dto.Port,
            UseHttps = dto.UseHttps,
            DoorNo = dto.DoorNo,
            FloorId = dto.FloorId,
            Direction = ParseDirection(dto.Direction),
            IsActive = true
        };
        await _devices.AddAsync(device, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("DEVICE_CREATED", $"{device.Name} ({device.Ip}) cihazı əlavə edildi.", "device", device.Id, ct: ct);
        return device.Id;
    }

    public async Task UpdateDeviceAsync(long id, DeviceInputDto dto, CancellationToken ct = default)
    {
        var device = await _devices.GetByIdAsync(id, ct)
                     ?? throw new KeyNotFoundException("Cihaz tapılmadı.");
        ValidateDevice(dto);
        if (await _devices.ExistsByIpPortAsync(dto.Ip.Trim(), dto.Port, id, ct))
            throw new ArgumentException("Bu IP:port ünvanı ilə başqa cihaz mövcuddur.");
        if (await _floors.GetByIdAsync(dto.FloorId, ct) is null)
            throw new ArgumentException("Mərtəbə tapılmadı.");

        device.Name = dto.Name.Trim();
        device.Ip = dto.Ip.Trim();
        device.Port = dto.Port;
        device.UseHttps = dto.UseHttps;
        device.DoorNo = dto.DoorNo;
        device.FloorId = dto.FloorId;
        device.Direction = ParseDirection(dto.Direction);
        device.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("DEVICE_UPDATED", $"{device.Name} ({device.Ip}) cihazı yeniləndi.", "device", device.Id, ct: ct);
    }

    public async Task ToggleDeviceAsync(long id, CancellationToken ct = default)
    {
        var device = await _devices.GetByIdAsync(id, ct)
                     ?? throw new KeyNotFoundException("Cihaz tapılmadı.");
        device.IsActive = !device.IsActive;
        device.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("DEVICE_STATUS_CHANGED",
            $"{device.Name} cihazı {(device.IsActive ? "aktiv" : "deaktiv")} edildi.", "device", device.Id, ct: ct);
    }

    public async Task DeleteDeviceAsync(long id, CancellationToken ct = default)
    {
        var device = await _devices.GetByIdAsync(id, ct)
                     ?? throw new KeyNotFoundException("Cihaz tapılmadı.");
        var name = device.Name;
        try
        {
            _devices.Remove(device);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Bu cihaz üçün qeydlər (enrollment) var, silinə bilməz. Onu deaktiv edin.");
        }
        await _log.LogAsync("DEVICE_DELETED", $"{name} cihazı silindi.", "device", id, ct: ct);
    }

    private static void ValidateDevice(DeviceInputDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Cihaz adını daxil edin.");
        if (string.IsNullOrWhiteSpace(dto.Ip))
            throw new ArgumentException("Cihaz IP ünvanını daxil edin.");
        if (dto.Port is < 1 or > 65535)
            throw new ArgumentException("Port 1–65535 aralığında olmalıdır.");
        if (dto.DoorNo < 1)
            throw new ArgumentException("Qapı nömrəsi 1-dən kiçik ola bilməz.");
        if (dto.FloorId <= 0)
            throw new ArgumentException("Mərtəbə seçilməlidir.");
    }

    private static DeviceDirection ParseDirection(string? d) =>
        string.Equals(d, "Exit", StringComparison.OrdinalIgnoreCase) ? DeviceDirection.Exit : DeviceDirection.Entry;
}
