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
