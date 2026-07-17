namespace AccessControlSystem.Domain.Entities;

/// <summary>Rol × Bölmə icazələri (Baxış / Əlavə / Redaktə / Silmə).</summary>
public class RolePermission
{
    public long Id { get; set; }

    public long RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public long SectionId { get; set; }
    public Section Section { get; set; } = null!;

    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}
