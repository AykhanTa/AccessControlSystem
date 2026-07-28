namespace AccessControlSystem.Domain.Entities;

/// <summary>Ziyarət ↔ Mərtəbə (çox-çoxa) — qonağın icazəli mərtəbələri.</summary>
public class VisitFloor
{
    public long VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public long FloorId { get; set; }
    public Floor Floor { get; set; } = null!;
}
