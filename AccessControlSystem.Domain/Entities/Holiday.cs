using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Qeyri-iş günü (dövlət bayramı və s.). Bu gün davamiyyətdə qayıb sayılmır.</summary>
public class Holiday : BaseEntity
{
    /// <summary>Sahibi şirkət (null = qlobal, hamıya aid).</summary>
    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    public DateTime Date { get; set; }               // yalnız tarix (00:00)
    public string Name { get; set; } = string.Empty; // "Novruz bayramı"
}
