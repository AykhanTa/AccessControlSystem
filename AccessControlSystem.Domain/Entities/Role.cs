using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Rol (Administrator, Mühafizə, Qəbul və ya xüsusi rollar).</summary>
public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>1 = qorunan sistem rolu (Administrator) — silinə/redaktə edilə bilməz.</summary>
    public bool IsSystem { get; set; }

    /// <summary>1 = Təhlükəsizlik məsulu rolu — bütün müəssisələrin giriş-çıxışını oxu-yönümlü görür.</summary>
    public bool IsSecurity { get; set; }

    public ICollection<RolePermission> Permissions { get; set; } = new List<RolePermission>();
    public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
}
