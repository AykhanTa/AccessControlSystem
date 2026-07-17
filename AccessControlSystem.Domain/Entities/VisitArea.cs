namespace AccessControlSystem.Domain.Entities;

/// <summary>Ziyarət ↔ Ərazi (çox-çoxa əlaqə).</summary>
public class VisitArea
{
    public long VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public long AreaId { get; set; }
    public Area Area { get; set; } = null!;
}
