using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;

namespace AccessControlSystem.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roles;
    private readonly ISectionRepository _sections;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public RoleService(IRoleRepository roles, ISectionRepository sections, IUnitOfWork uow, ISystemLogWriter log)
    {
        _roles = roles;
        _sections = sections;
        _uow = uow;
        _log = log;
    }

    public async Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var roles = await _roles.GetAllWithPermissionsAsync(ct);
        var sections = await _sections.GetAllOrderedAsync(ct);

        var result = new List<RoleDto>();
        foreach (var role in roles)
        {
            var perms = sections.Select(s =>
            {
                var rp = role.Permissions.FirstOrDefault(p => p.SectionId == s.Id);
                return new SectionPermDto
                {
                    SectionId = s.Id,
                    SectionCode = s.Code,
                    SectionName = s.Name,
                    CanView = rp?.CanView ?? false,
                    CanAdd = rp?.CanAdd ?? false,
                    CanEdit = rp?.CanEdit ?? false,
                    CanDelete = rp?.CanDelete ?? false
                };
            }).ToList();

            result.Add(new RoleDto
            {
                Id = role.Id,
                Name = role.Name,
                Description = role.Description,
                IsSystem = role.IsSystem,
                UserCount = await _roles.CountUsersAsync(role.Id, ct),
                Permissions = perms
            });
        }
        return result;
    }

    public async Task<List<LookupDto>> GetRoleOptionsAsync(CancellationToken ct = default)
    {
        var roles = await _roles.GetAllWithPermissionsAsync(ct);
        return roles.Select(r => new LookupDto { Id = r.Id, Name = r.Name }).ToList();
    }

    public async Task<long> CreateAsync(RoleCreateDto dto, CancellationToken ct = default)
    {
        var name = (dto.Name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Rolun adını daxil edin.");
        if (await _roles.ExistsByNameAsync(name, null, ct))
            throw new ArgumentException("Bu adda rol artıq mövcuddur.");

        // Yeni rol bütün bölmələrdə bağlı (icazəsiz) yaradılır
        var sections = await _sections.GetAllOrderedAsync(ct);
        var role = new Role
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(dto.Description) ? "Xüsusi rol" : dto.Description.Trim(),
            IsSystem = false,
            Permissions = sections.Select(s => new RolePermission { SectionId = s.Id }).ToList()
        };
        await _roles.AddAsync(role, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("ROLE_CREATED", $"{role.Name} rolu yaradıldı.", "role", role.Id, ct: ct);
        return role.Id;
    }

    public async Task UpdatePermissionsAsync(long roleId, List<SectionPermDto> permissions, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(roleId, ct)
                   ?? throw new KeyNotFoundException("Rol tapılmadı.");
        if (role.IsSystem)
            throw new InvalidOperationException("Administrator rolunun icazələri dəyişdirilə bilməz.");

        foreach (var input in permissions)
        {
            var rp = role.Permissions.FirstOrDefault(p => p.SectionId == input.SectionId);
            if (rp is null)
            {
                rp = new RolePermission { RoleId = role.Id, SectionId = input.SectionId };
                role.Permissions.Add(rp);
            }
            rp.CanView = input.CanView;
            rp.CanAdd = input.CanAdd;
            rp.CanEdit = input.CanEdit;
            rp.CanDelete = input.CanDelete;
        }
        role.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("ROLE_PERMISSION_UPDATED", $"{role.Name} rolunun icazələri yeniləndi.", "role", role.Id, ct: ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(id, ct)
                   ?? throw new KeyNotFoundException("Rol tapılmadı.");
        if (role.IsSystem)
            throw new InvalidOperationException("Sistem rolu silinə bilməz.");
        if (await _roles.CountUsersAsync(id, ct) > 0)
            throw new InvalidOperationException("Bu rola təyin edilmiş istifadəçilər var. Əvvəlcə onların rolunu dəyişin.");

        var name = role.Name;
        _roles.Remove(role);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("ROLE_DELETED", $"{name} rolu silindi.", "role", id, ct: ct);
    }
}
