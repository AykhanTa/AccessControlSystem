using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Giriş ərazisi.</summary>
public class Area : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<VisitArea> VisitAreas { get; set; } = new List<VisitArea>();
}
