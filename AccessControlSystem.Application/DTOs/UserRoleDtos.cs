namespace AccessControlSystem.Application.DTOs;

// ---------- İstifadəçilər ----------

public class UserDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string Status { get; set; } = "active";   // "active" | "inactive"
    public bool IsProtected { get; set; }
    public bool IsGlobalAdmin { get; set; }           // qorunan + Administrator rolu
    public long? CompanyId { get; set; }
    public string? CompanyName { get; set; }
}

public class UserCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public long RoleId { get; set; }
    /// <summary>Aid olacağı şirkət (yalnız qlobal admin seçir; şirkət istifadəçisində öz şirkəti tətbiq olunur).</summary>
    public long? CompanyId { get; set; }
}

public class UserUpdateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string? Password { get; set; }   // boş ola bilər (dəyişdirilmir)
}

// ---------- Rollar ----------

/// <summary>Bir bölmə üçün rol icazələri.</summary>
public class SectionPermDto
{
    public long SectionId { get; set; }
    public string SectionCode { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}

public class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int UserCount { get; set; }
    public List<SectionPermDto> Permissions { get; set; } = new();
}

public class RoleCreateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
