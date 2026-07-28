using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Repositories;

public class GuestRepository : IGuestRepository
{
    private readonly AppDbContext _db;
    public GuestRepository(AppDbContext db) => _db = db;

    public Task<Guest?> GetByDocumentAsync(string idDocument, CancellationToken ct = default) =>
        _db.Guests.FirstOrDefaultAsync(g => g.IdDocument == idDocument, ct);

    public async Task AddAsync(Guest guest, CancellationToken ct = default) =>
        await _db.Guests.AddAsync(guest, ct);
}

public class HostRepository : IHostRepository
{
    private readonly AppDbContext _db;
    public HostRepository(AppDbContext db) => _db = db;

    public Task<List<Host>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Hosts.Where(h => h.IsActive).OrderBy(h => h.FirstName).ToListAsync(ct);

    public Task<List<Host>> GetAllAsync(CancellationToken ct = default) =>
        _db.Hosts.OrderBy(h => h.FirstName).ThenBy(h => h.LastName).ToListAsync(ct);

    public Task<Host?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Hosts.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<bool> ExistsAsync(long id, CancellationToken ct = default) =>
        _db.Hosts.AnyAsync(h => h.Id == id, ct);

    public async Task AddAsync(Host host, CancellationToken ct = default) =>
        await _db.Hosts.AddAsync(host, ct);

    public void Remove(Host host) => _db.Hosts.Remove(host);
}

public class AreaRepository : IAreaRepository
{
    private readonly AppDbContext _db;
    public AreaRepository(AppDbContext db) => _db = db;

    public Task<List<Area>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Areas.Where(a => a.IsActive).OrderBy(a => a.Name).ToListAsync(ct);

    public Task<List<Area>> GetAllAsync(CancellationToken ct = default) =>
        _db.Areas.OrderBy(a => a.Name).ToListAsync(ct);

    public Task<Area?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Areas.FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<List<Area>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default) =>
        _db.Areas.Where(a => ids.Contains(a.Id)).ToListAsync(ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        _db.Areas.AnyAsync(a => a.Name == name, ct);

    public Task<int> UsageCountAsync(long areaId, CancellationToken ct = default) =>
        _db.VisitAreas.CountAsync(va => va.AreaId == areaId, ct);

    public async Task AddAsync(Area area, CancellationToken ct = default) =>
        await _db.Areas.AddAsync(area, ct);

    public void Remove(Area area) => _db.Areas.Remove(area);
}

public class FloorRepository : IFloorRepository
{
    private readonly AppDbContext _db;
    public FloorRepository(AppDbContext db) => _db = db;

    public Task<List<Floor>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Floors.Where(f => f.IsActive).OrderBy(f => f.Name).ToListAsync(ct);

    public Task<List<Floor>> GetAllAsync(CancellationToken ct = default) =>
        _db.Floors.OrderBy(f => f.Name).ToListAsync(ct);

    public Task<List<Floor>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default) =>
        _db.Floors.Where(f => ids.Contains(f.Id)).ToListAsync(ct);

    public Task<Floor?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Floors.FirstOrDefaultAsync(f => f.Id == id, ct);

    public Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default) =>
        _db.Floors.AnyAsync(f => f.Name == name && (excludeId == null || f.Id != excludeId), ct);

    public Task<int> DeviceCountAsync(long floorId, CancellationToken ct = default) =>
        _db.Devices.CountAsync(d => d.FloorId == floorId, ct);

    public Task<int> VisitUsageCountAsync(long floorId, CancellationToken ct = default) =>
        _db.VisitFloors.CountAsync(vf => vf.FloorId == floorId, ct);

    public async Task AddAsync(Floor floor, CancellationToken ct = default) =>
        await _db.Floors.AddAsync(floor, ct);

    public void Remove(Floor floor) => _db.Floors.Remove(floor);
}

public class DeviceRepository : IDeviceRepository
{
    private readonly AppDbContext _db;
    public DeviceRepository(AppDbContext db) => _db = db;

    public Task<List<Device>> GetActiveByFloorIdsAsync(IEnumerable<long> floorIds, CancellationToken ct = default) =>
        _db.Devices
            .Where(d => d.IsActive && floorIds.Contains(d.FloorId))
            .ToListAsync(ct);

    public Task<Device?> GetByIpAsync(string ip, CancellationToken ct = default) =>
        _db.Devices.Include(d => d.Floor).FirstOrDefaultAsync(d => d.Ip == ip, ct);

    public Task<List<Device>> GetAllActiveAsync(CancellationToken ct = default) =>
        _db.Devices.Where(d => d.IsActive).ToListAsync(ct);

    public Task<List<Device>> GetAllWithFloorAsync(CancellationToken ct = default) =>
        _db.Devices.Include(d => d.Floor).OrderBy(d => d.Floor.Name).ThenBy(d => d.Name).ToListAsync(ct);

    public Task<Device?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Devices.FirstOrDefaultAsync(d => d.Id == id, ct);

    public Task<bool> ExistsByIpPortAsync(string ip, int port, long? excludeId = null, CancellationToken ct = default) =>
        _db.Devices.AnyAsync(d => d.Ip == ip && d.Port == port && (excludeId == null || d.Id != excludeId), ct);

    public async Task AddAsync(Device device, CancellationToken ct = default) =>
        await _db.Devices.AddAsync(device, ct);

    public void Remove(Device device) => _db.Devices.Remove(device);
}

public class DeviceEnrollmentRepository : IDeviceEnrollmentRepository
{
    private readonly AppDbContext _db;
    public DeviceEnrollmentRepository(AppDbContext db) => _db = db;

    public Task<List<DeviceEnrollment>> GetByVisitAsync(long visitId, CancellationToken ct = default) =>
        _db.DeviceEnrollments
            .Include(e => e.Device)
            .Include(e => e.Visit).ThenInclude(v => v.Guest)
            .Where(e => e.VisitId == visitId)
            .ToListAsync(ct);

    public async Task AddAsync(DeviceEnrollment enrollment, CancellationToken ct = default) =>
        await _db.DeviceEnrollments.AddAsync(enrollment, ct);
}

public class AccessEventRepository : IAccessEventRepository
{
    private readonly AppDbContext _db;
    public AccessEventRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AccessEvent ev, CancellationToken ct = default) =>
        await _db.AccessEvents.AddAsync(ev, ct);

    public Task<List<AccessEvent>> GetRecentAsync(int take, CancellationToken ct = default) =>
        _db.AccessEvents.Include(e => e.Device)
            .OrderByDescending(e => e.Id).Take(take).ToListAsync(ct);
}

public class PurposeRepository : IPurposeRepository
{
    private readonly AppDbContext _db;
    public PurposeRepository(AppDbContext db) => _db = db;

    public Task<List<Purpose>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Purposes.Where(p => p.IsActive).OrderBy(p => p.Name).ToListAsync(ct);

    public Task<List<Purpose>> GetAllAsync(CancellationToken ct = default) =>
        _db.Purposes.OrderBy(p => p.Name).ToListAsync(ct);

    public Task<Purpose?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Purposes.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<List<Purpose>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default) =>
        _db.Purposes.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    public Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default) =>
        _db.Purposes.AnyAsync(p => p.Name == name, ct);

    public async Task AddAsync(Purpose purpose, CancellationToken ct = default) =>
        await _db.Purposes.AddAsync(purpose, ct);
}
