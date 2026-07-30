namespace AccessControlSystem.Domain.Entities;

/// <summary>İşçi ↔ Mərtəbə (çox-çoxa) — işçinin icazəli mərtəbələri.</summary>
public class EmployeeFloor
{
    public long EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public long FloorId { get; set; }
    public Floor Floor { get; set; } = null!;
}
