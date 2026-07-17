using System.Text.RegularExpressions;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IPasswordHasher _hasher;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public UserService(IUserRepository users, IRoleRepository roles, IPasswordHasher hasher,
                       IUnitOfWork uow, ISystemLogWriter log)
    {
        _users = users;
        _roles = roles;
        _hasher = hasher;
        _uow = uow;
        _log = log;
    }

    public async Task<List<UserDto>> GetAllAsync(CancellationToken ct = default)
    {
        var users = await _users.GetAllWithRoleAsync(ct);
        return users.Select(ToDto).ToList();
    }

    public async Task<long> CreateAsync(UserCreateDto dto, CancellationToken ct = default)
    {
        Validate(dto.FirstName, dto.LastName, dto.Email);
        if (string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Şifrə daxil edin.");
        if (await _users.ExistsByEmailAsync(dto.Email.Trim(), null, ct))
            throw new ArgumentException("Bu email artıq mövcuddur.");
        var role = await _roles.GetByIdWithPermissionsAsync(dto.RoleId, ct)
                   ?? throw new ArgumentException("Rol seçin.");

        var user = new AppUser
        {
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = _hasher.Hash(dto.Password),
            RoleId = role.Id,
            Status = UserStatus.Active,
            IsProtected = false
        };
        await _users.AddAsync(user, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("USER_CREATED", $"{user.FullName} istifadəçisi əlavə edildi.", "user", user.Id, ct: ct);
        return user.Id;
    }

    public async Task UpdateAsync(long id, UserUpdateDto dto, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
        if (user.IsProtected)
            throw new InvalidOperationException("Qorunan hesab redaktə edilə bilməz.");

        Validate(dto.FirstName, dto.LastName, dto.Email);
        if (await _users.ExistsByEmailAsync(dto.Email.Trim(), id, ct))
            throw new ArgumentException("Bu email başqa istifadəçidə mövcuddur.");
        _ = await _roles.GetByIdWithPermissionsAsync(dto.RoleId, ct)
            ?? throw new ArgumentException("Rol seçin.");

        user.FirstName = dto.FirstName.Trim();
        user.LastName = dto.LastName.Trim();
        user.Email = dto.Email.Trim();
        user.RoleId = dto.RoleId;
        if (!string.IsNullOrWhiteSpace(dto.Password))
            user.PasswordHash = _hasher.Hash(dto.Password);
        user.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("USER_UPDATED", $"{user.FullName} istifadəçisi yeniləndi.", "user", user.Id, ct: ct);
    }

    public async Task ToggleStatusAsync(long id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
        if (user.IsProtected)
            throw new InvalidOperationException("Qorunan hesabın statusu dəyişdirilə bilməz.");

        user.Status = user.Status == UserStatus.Active ? UserStatus.Inactive : UserStatus.Active;
        user.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("USER_STATUS_CHANGED",
            $"{user.FullName} istifadəçisi {(user.Status == UserStatus.Active ? "aktiv" : "deaktiv")} edildi.", "user", user.Id, ct: ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var user = await _users.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("İstifadəçi tapılmadı.");
        if (user.IsProtected)
            throw new InvalidOperationException("Qorunan hesab silinə bilməz.");

        var name = user.FullName;
        _users.Remove(user);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("USER_DELETED", $"{name} istifadəçisi silindi.", "user", id, ct: ct);
    }

    private static void Validate(string firstName, string lastName, string email)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Ad və Soyad boş ola bilməz.");
        if (!Regex.IsMatch(email ?? "", @"^[^\s@]+@[^\s@]+\.[^\s@]+$"))
            throw new ArgumentException("Düzgün email daxil edin.");
    }

    private static UserDto ToDto(AppUser u) => new()
    {
        Id = u.Id,
        FullName = u.FullName,
        FirstName = u.FirstName,
        LastName = u.LastName,
        Email = u.Email,
        RoleId = u.RoleId,
        RoleName = u.Role?.Name ?? string.Empty,
        Status = u.Status == UserStatus.Active ? "active" : "inactive",
        IsProtected = u.IsProtected,
        IsGlobalAdmin = u.IsProtected && (u.Role?.IsSystem ?? false)
    };
}
