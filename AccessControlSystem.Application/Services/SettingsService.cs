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
    private readonly ICenterRepository _centers;
    private readonly IFloorRepository _floors;
    private readonly IDeviceRepository _devices;
    private readonly ICompanyRepository _companies;
    private readonly IDepartmentRepository _departments;
    private readonly IPositionRepository _positions;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;
    private readonly ICurrentTenant _tenant;

    public SettingsService(IHostRepository hosts, IAreaRepository areas, IPurposeRepository purposes,
                           ICenterRepository centers, IFloorRepository floors, IDeviceRepository devices,
                           ICompanyRepository companies, IDepartmentRepository departments, IPositionRepository positions,
                           IUnitOfWork uow, ISystemLogWriter log, ICurrentTenant tenant)
    {
        _hosts = hosts;
        _areas = areas;
        _purposes = purposes;
        _centers = centers;
        _floors = floors;
        _devices = devices;
        _companies = companies;
        _departments = departments;
        _positions = positions;
        _uow = uow;
        _log = log;
        _tenant = tenant;
    }

    /// <summary>Yeni obyekt üçün sahib şirkəti təyin edir: şirkət istifadəçisi → öz şirkəti;
    /// qlobal admin → formdakı seçim (verilməyibsə null = qlobal).</summary>
    private long? OwnerCompanyId(long? formCompanyId = null) =>
        _tenant.IsGlobalAdmin ? formCompanyId : _tenant.CompanyId;

    // ---------- Şirkətlər ----------

    public async Task<List<CompanyItemDto>> GetCompaniesAsync(CancellationToken ct = default)
    {
        var companies = await _companies.GetAllAsync(ct);
        var result = new List<CompanyItemDto>();
        foreach (var c in companies)
            result.Add(new CompanyItemDto
            {
                Id = c.Id, Name = c.Name, TaxNumber = c.TaxNumber, Phone = c.Phone, IsActive = c.IsActive,
                DepartmentCount = await _companies.DepartmentCountAsync(c.Id, ct)
            });
        return result;
    }

    public async Task<long> AddCompanyAsync(CompanyInputDto dto, CancellationToken ct = default)
    {
        if (!_tenant.IsGlobalAdmin)
            throw new InvalidOperationException("Şirkət yalnız qlobal admin tərəfindən əlavə edilə bilər.");
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Şirkət adını daxil edin.");
        var company = new Company
        {
            Name = name, TaxNumber = dto.TaxNumber?.Trim(), ContactPerson = dto.ContactPerson?.Trim(),
            Phone = dto.Phone?.Trim(), Email = dto.Email?.Trim(), IsActive = true
        };
        await _companies.AddAsync(company, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("COMPANY_CREATED", $"{company.Name} şirkəti əlavə edildi.", "company", company.Id, ct: ct);
        return company.Id;
    }

    public async Task ToggleCompanyAsync(long id, CancellationToken ct = default)
    {
        var company = await _companies.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Şirkət tapılmadı.");
        company.IsActive = !company.IsActive;
        company.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("COMPANY_STATUS_CHANGED",
            $"{company.Name} şirkəti {(company.IsActive ? "aktiv" : "deaktiv")} edildi.", "company", company.Id, ct: ct);
    }

    public async Task DeleteCompanyAsync(long id, CancellationToken ct = default)
    {
        var company = await _companies.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Şirkət tapılmadı.");
        if (await _companies.HasDependentsAsync(id, ct))
            throw new InvalidOperationException("Bu şirkətdə şöbə/vəzifə var. Əvvəlcə onları silin.");
        var name = company.Name;
        _companies.Remove(company);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("COMPANY_DELETED", $"{name} şirkəti silindi.", "company", id, ct: ct);
    }

    // ---------- Şöbələr ----------

    public async Task<List<DepartmentItemDto>> GetDepartmentsAsync(CancellationToken ct = default) =>
        (await _departments.GetAllWithCompanyAsync(ct)).Select(d => new DepartmentItemDto
        {
            Id = d.Id, Name = d.Name, CompanyId = d.CompanyId, CompanyName = d.Company?.Name ?? "",
            ParentName = d.ParentDepartment?.Name, IsActive = d.IsActive
        }).ToList();

    public async Task<long> AddDepartmentAsync(string name, long companyId, long? parentId, CancellationToken ct = default)
    {
        companyId = OwnerCompanyId(companyId) ?? companyId;   // şirkət istifadəçisi → öz şirkəti
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Şöbə adını daxil edin.");
        if (await _companies.GetByIdAsync(companyId, ct) is null)
            throw new ArgumentException("Şirkət seçilməlidir.");
        var dep = new Department { Name = name, CompanyId = companyId, ParentDepartmentId = parentId, IsActive = true };
        await _departments.AddAsync(dep, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("DEPARTMENT_CREATED", $"{dep.Name} şöbəsi əlavə edildi.", "department", dep.Id, ct: ct);
        return dep.Id;
    }

    public async Task ToggleDepartmentAsync(long id, CancellationToken ct = default)
    {
        var dep = await _departments.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Şöbə tapılmadı.");
        dep.IsActive = !dep.IsActive;
        dep.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("DEPARTMENT_STATUS_CHANGED",
            $"{dep.Name} şöbəsi {(dep.IsActive ? "aktiv" : "deaktiv")} edildi.", "department", dep.Id, ct: ct);
    }

    public async Task DeleteDepartmentAsync(long id, CancellationToken ct = default)
    {
        var dep = await _departments.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Şöbə tapılmadı.");
        var name = dep.Name;
        try
        {
            _departments.Remove(dep);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Bu şöbə istifadə olunub (alt şöbə/işçi), silinə bilməz. Deaktiv edin.");
        }
        await _log.LogAsync("DEPARTMENT_DELETED", $"{name} şöbəsi silindi.", "department", id, ct: ct);
    }

    // ---------- Vəzifələr ----------

    public async Task<List<PositionItemDto>> GetPositionsAsync(CancellationToken ct = default) =>
        (await _positions.GetAllWithCompanyAsync(ct)).Select(p => new PositionItemDto
        {
            Id = p.Id, Name = p.Name, CompanyId = p.CompanyId, CompanyName = p.Company?.Name ?? "", IsActive = p.IsActive
        }).ToList();

    public async Task<long> AddPositionAsync(string name, long companyId, CancellationToken ct = default)
    {
        companyId = OwnerCompanyId(companyId) ?? companyId;   // şirkət istifadəçisi → öz şirkəti
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Vəzifə adını daxil edin.");
        if (await _companies.GetByIdAsync(companyId, ct) is null)
            throw new ArgumentException("Şirkət seçilməlidir.");
        var pos = new Position { Name = name, CompanyId = companyId, IsActive = true };
        await _positions.AddAsync(pos, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("POSITION_CREATED", $"{pos.Name} vəzifəsi əlavə edildi.", "position", pos.Id, ct: ct);
        return pos.Id;
    }

    public async Task TogglePositionAsync(long id, CancellationToken ct = default)
    {
        var pos = await _positions.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Vəzifə tapılmadı.");
        pos.IsActive = !pos.IsActive;
        pos.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("POSITION_STATUS_CHANGED",
            $"{pos.Name} vəzifəsi {(pos.IsActive ? "aktiv" : "deaktiv")} edildi.", "position", pos.Id, ct: ct);
    }

    public async Task DeletePositionAsync(long id, CancellationToken ct = default)
    {
        var pos = await _positions.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Vəzifə tapılmadı.");
        var name = pos.Name;
        try
        {
            _positions.Remove(pos);
            await _uow.SaveChangesAsync(ct);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Bu vəzifə işçilərdə istifadə olunub, silinə bilməz. Deaktiv edin.");
        }
        await _log.LogAsync("POSITION_DELETED", $"{name} vəzifəsi silindi.", "position", id, ct: ct);
    }

    // ---------- Mərkəzlər (binalar) ----------

    public async Task<List<CenterItemDto>> GetCentersAsync(CancellationToken ct = default)
    {
        var centers = await _centers.GetAllAsync(ct);
        var result = new List<CenterItemDto>();
        foreach (var c in centers)
            result.Add(new CenterItemDto
            {
                Id = c.Id, Code = c.Code, Name = c.Name, City = c.City, IsActive = c.IsActive,
                FloorCount = await _centers.FloorCountAsync(c.Id, ct)
            });
        return result;
    }

    public async Task<long> AddCenterAsync(CenterInputDto dto, CancellationToken ct = default)
    {
        if (!_tenant.IsGlobalAdmin)
            throw new InvalidOperationException("Mərkəz yalnız qlobal admin tərəfindən əlavə edilə bilər.");
        var code = (dto.Code ?? string.Empty).Trim();
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mərkəz kodu və adı daxil edin.");
        if (await _centers.ExistsByCodeAsync(code, null, ct))
            throw new ArgumentException("Bu kodla mərkəz artıq mövcuddur.");
        var center = new Center { Code = code, Name = name, Address = dto.Address?.Trim(), City = dto.City?.Trim(), IsActive = true, CompanyId = OwnerCompanyId(dto.CompanyId) };
        await _centers.AddAsync(center, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CENTER_CREATED", $"{center.Name} mərkəzi əlavə edildi.", "center", center.Id, ct: ct);
        return center.Id;
    }

    public async Task UpdateCenterAsync(long id, CenterInputDto dto, CancellationToken ct = default)
    {
        var center = await _centers.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Mərkəz tapılmadı.");
        var code = (dto.Code ?? string.Empty).Trim();
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mərkəz kodu və adı boş ola bilməz.");
        if (await _centers.ExistsByCodeAsync(code, id, ct))
            throw new ArgumentException("Bu kodla başqa mərkəz mövcuddur.");
        center.Code = code; center.Name = name; center.Address = dto.Address?.Trim(); center.City = dto.City?.Trim();
        center.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CENTER_UPDATED", $"{center.Name} mərkəzi yeniləndi.", "center", center.Id, ct: ct);
    }

    public async Task ToggleCenterAsync(long id, CancellationToken ct = default)
    {
        var center = await _centers.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Mərkəz tapılmadı.");
        center.IsActive = !center.IsActive;
        center.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CENTER_STATUS_CHANGED",
            $"{center.Name} mərkəzi {(center.IsActive ? "aktiv" : "deaktiv")} edildi.", "center", center.Id, ct: ct);
    }

    public async Task DeleteCenterAsync(long id, CancellationToken ct = default)
    {
        var center = await _centers.GetByIdAsync(id, ct) ?? throw new KeyNotFoundException("Mərkəz tapılmadı.");
        if (await _centers.FloorCountAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu mərkəzdə mərtəbələr var. Əvvəlcə onları başqa mərkəzə köçürün və ya silin.");
        var name = center.Name;
        _centers.Remove(center);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CENTER_DELETED", $"{name} mərkəzi silindi.", "center", id, ct: ct);
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
            IsActive = true,
            CompanyId = OwnerCompanyId()
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

    public async Task DeletePurposeAsync(long id, CancellationToken ct = default)
    {
        var purpose = await _purposes.GetByIdAsync(id, ct)
                      ?? throw new KeyNotFoundException("Məqsəd tapılmadı.");
        if (await _purposes.UsageCountAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu məqsəd ziyarətlərdə istifadə olunub — silinə bilməz (deaktiv edin).");
        _purposes.Remove(purpose);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("PURPOSE_DELETED", $"{purpose.Name} məqsədi silindi.", "purpose", id, ct: ct);
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
                DeviceCount = await _floors.DeviceCountAsync(f.Id, ct),
                CenterId = f.CenterId, CenterName = f.Center?.Name
            });
        return result;
    }

    public async Task<long> AddFloorAsync(string name, long? centerId, CancellationToken ct = default)
    {
        name = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Mərtəbə adını daxil edin.");
        if (await _floors.ExistsByNameAsync(name, null, ct))
            throw new ArgumentException("Bu mərtəbə artıq mövcuddur.");
        var floor = new Floor { Name = name, CenterId = centerId, IsActive = true, CompanyId = OwnerCompanyId() };
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
            Direction = d.Direction.ToString(),
            PointType = (d.AccessPoint?.PointType ?? PointType.Door).ToString(),
            IsActive = d.IsActive
        }).ToList();

    public async Task<long> AddDeviceAsync(DeviceInputDto dto, CancellationToken ct = default)
    {
        ValidateDevice(dto);
        if (await _devices.ExistsByIpPortAsync(dto.Ip.Trim(), dto.Port, null, ct))
            throw new ArgumentException("Bu IP:port ünvanı ilə cihaz artıq mövcuddur.");
        var floor = await _floors.GetByIdAsync(dto.FloorId, ct)
                    ?? throw new ArgumentException("Mərtəbə tapılmadı.");

        var direction = ParseDirection(dto.Direction);
        var pointType = ParsePointType(dto.PointType);
        var owner = OwnerCompanyId() ?? floor.CompanyId;   // cihaz mərtəbənin şirkətini miras alır
        var device = new Device
        {
            Name = dto.Name.Trim(),
            Ip = dto.Ip.Trim(),
            Port = dto.Port,
            UseHttps = dto.UseHttps,
            DoorNo = dto.DoorNo,
            FloorId = dto.FloorId,
            Direction = direction,
            IsActive = true,
            CompanyId = owner,
            // Cihaza uyğun keçid nöqtəsi (1:1) avtomatik yaradılır.
            AccessPoint = new AccessPoint
            {
                Name = dto.Name.Trim(),
                FloorId = dto.FloorId,
                CenterId = floor.CenterId,
                Direction = direction,
                PointType = pointType,
                IsActive = true,
                CompanyId = owner
            }
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
        var floor = await _floors.GetByIdAsync(dto.FloorId, ct)
                    ?? throw new ArgumentException("Mərtəbə tapılmadı.");

        var direction = ParseDirection(dto.Direction);
        var pointType = ParsePointType(dto.PointType);
        device.Name = dto.Name.Trim();
        device.Ip = dto.Ip.Trim();
        device.Port = dto.Port;
        device.UseHttps = dto.UseHttps;
        device.DoorNo = dto.DoorNo;
        device.FloorId = dto.FloorId;
        device.Direction = direction;
        device.UpdatedAt = DateTime.Now;

        // Bağlı keçid nöqtəsini yenilə (yoxdursa yarat).
        device.AccessPoint ??= new AccessPoint { FloorId = dto.FloorId, IsActive = true };
        device.AccessPoint.Name = dto.Name.Trim();
        device.AccessPoint.FloorId = dto.FloorId;
        device.AccessPoint.CenterId = floor.CenterId;
        device.AccessPoint.CompanyId = device.CompanyId;
        device.AccessPoint.Direction = direction;
        device.AccessPoint.PointType = pointType;
        device.AccessPoint.UpdatedAt = DateTime.Now;

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

    private static PointType ParsePointType(string? p) =>
        Enum.TryParse<PointType>(p, true, out var pt) ? pt : PointType.Door;
}
