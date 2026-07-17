namespace AccessControlSystem.Domain.Entities;

/// <summary>Sistem bölməsi — icazə matrisinin sətirləri (Ana səhifə, Qonaqlar, ...).</summary>
public class Section
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;   // 'dashboard', 'guests', ...
    public string Name { get; set; } = string.Empty;   // görünən ad
    public int SortOrder { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
