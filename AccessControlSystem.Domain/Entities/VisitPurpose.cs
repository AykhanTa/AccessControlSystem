namespace AccessControlSystem.Domain.Entities;

/// <summary>Ziyarət ↔ Məqsəd (çox-çoxa əlaqə).</summary>
public class VisitPurpose
{
    public long VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public long PurposeId { get; set; }
    public Purpose Purpose { get; set; } = null!;
}
