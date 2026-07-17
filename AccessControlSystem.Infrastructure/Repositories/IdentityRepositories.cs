using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly AppDbContext _db;
    public SectionRepository(AppDbContext db) => _db = db;

    public Task<List<Section>> GetAllOrderedAsync(CancellationToken ct = default) =>
        _db.Sections.OrderBy(s => s.SortOrder).ToListAsync(ct);
}

public class RoleRepository : IRoleRepository
{
    private readonly AppDbContext _db;
    public RoleRepository(AppDbContext db) => _db = db;

    public Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken ct = default) =>
        _db.Roles.Include(r => r.Permissions).OrderBy(r => r.Id).ToListAsync(ct);

    public Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken ct = default) =>
        _db.Roles.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.Name == name, ct);

    public Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default) =>
        _db.Roles.AnyAsync(r => r.Name == name && (excludeId == null || r.Id != excludeId), ct);

    public Task<int> CountUsersAsync(long roleId, CancellationToken ct = default) =>
        _db.Users.CountAsync(u => u.RoleId == roleId, ct);

    public async Task AddAsync(Role role, CancellationToken ct = default) =>
        await _db.Roles.AddAsync(role, ct);

    public void Remove(Role role) => _db.Roles.Remove(role);
}

public class SystemLogRepository : ISystemLogRepository
{
    private readonly AppDbContext _db;
    public SystemLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(SystemLog log, CancellationToken ct = default) =>
        await _db.SystemLogs.AddAsync(log, ct);

    private IQueryable<SystemLog> Filtered(string? search)
    {
        var q = _db.SystemLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(l => l.Action.Contains(s) || l.Description.Contains(s) || l.UserName.Contains(s));
        }
        return q;
    }

    public Task<List<SystemLog>> GetPagedAsync(string? search, int skip, int take, CancellationToken ct = default) =>
        Filtered(search)
            .OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct);

    public Task<int> CountAsync(string? search, CancellationToken ct = default) =>
        Filtered(search).CountAsync(ct);
}

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;
    public UserRepository(AppDbContext db) => _db = db;

    public Task<List<AppUser>> GetAllWithRoleAsync(CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).OrderByDescending(u => u.IsProtected).ThenBy(u => u.Id).ToListAsync(ct);

    public Task<AppUser?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email, ct);

    public Task<bool> ExistsByEmailAsync(string email, long? excludeId = null, CancellationToken ct = default) =>
        _db.Users.AnyAsync(u => u.Email == email && (excludeId == null || u.Id != excludeId), ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) => _db.Users.AnyAsync(ct);

    public async Task AddAsync(AppUser user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public void Remove(AppUser user) => _db.Users.Remove(user);
}
