using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Sistem istifadəçisi (sistemə daxil olanlar).</summary>
public class AppUser : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;   // açıq şifrə saxlanılmır

    public long RoleId { get; set; }
    public Role Role { get; set; } = null!;

    /// <summary>Aid olduğu şirkət. null = qlobal admin (bütün şirkətləri görür/idarə edir).</summary>
    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    public UserStatus Status { get; set; } = UserStatus.Active;
    /// <summary>1 = Global administrator (silinə/redaktə edilə bilməz).</summary>
    public bool IsProtected { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
