using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly ISectionRepository _sections;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public AuthService(IUserRepository users, IRoleRepository roles, ISectionRepository sections,
                       IPasswordHasher hasher, IUnitOfWork uow, ISystemLogWriter log)
    {
        _users = users;
        _roles = roles;
        _sections = sections;
        _hasher = hasher;
        _uow = uow;
        _log = log;
    }

    public async Task<AuthResultDto> AuthenticateAsync(string email, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return AuthResultDto.Fail("E-poçt və şifrəni daxil edin.");

        var user = await _users.GetByEmailAsync(email.Trim(), ct);
        if (user is null || !_hasher.Verify(password, user.PasswordHash))
            return AuthResultDto.Fail("E-poçt və ya şifrə yanlışdır.");
        if (user.Status != UserStatus.Active)
            return AuthResultDto.Fail("Bu hesab deaktiv edilib. Administratora müraciət edin.");

        user.LastLoginAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("LOGIN", $"{user.FullName} sistemə daxil oldu.", "user", user.Id,
            actorUserId: user.Id, actorName: user.FullName, ct: ct);

        return AuthResultDto.Ok(new AuthUserDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = user.Role?.Name ?? string.Empty,
            IsGlobalAdmin = user.IsProtected && (user.Role?.IsSystem ?? false),
            CompanyId = user.CompanyId
        });
    }

    public async Task<Dictionary<string, SectionAccessDto>> GetPermissionMapAsync(long roleId, CancellationToken ct = default)
    {
        var role = await _roles.GetByIdWithPermissionsAsync(roleId, ct);
        var sections = await _sections.GetAllOrderedAsync(ct);

        var map = new Dictionary<string, SectionAccessDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sections)
        {
            var rp = role?.Permissions.FirstOrDefault(p => p.SectionId == s.Id);
            map[s.Code] = new SectionAccessDto
            {
                CanView = rp?.CanView ?? false,
                CanAdd = rp?.CanAdd ?? false,
                CanEdit = rp?.CanEdit ?? false,
                CanDelete = rp?.CanDelete ?? false
            };
        }
        return map;
    }
}
