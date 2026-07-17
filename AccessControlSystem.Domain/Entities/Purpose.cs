using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Gəliş məqsədi.</summary>
public class Purpose : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<VisitPurpose> VisitPurposes { get; set; } = new List<VisitPurpose>();
}
