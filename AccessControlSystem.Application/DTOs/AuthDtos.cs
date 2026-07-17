namespace AccessControlSystem.Application.DTOs;

public class AuthUserDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public long RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public bool IsGlobalAdmin { get; set; }
}

/// <summary>Bir bölmə üçün cari istifadəçinin icazələri.</summary>
public class SectionAccessDto
{
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }

    public static SectionAccessDto All() => new() { CanView = true, CanAdd = true, CanEdit = true, CanDelete = true };
}

public class AuthResultDto
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public AuthUserDto? User { get; set; }

    public static AuthResultDto Fail(string error) => new() { Success = false, Error = error };
    public static AuthResultDto Ok(AuthUserDto user) => new() { Success = true, User = user };
}
