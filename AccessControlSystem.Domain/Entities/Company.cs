using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Şirkət — işçilərin, şöbələrin və vəzifələrin aid olduğu təşkilat.</summary>
public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }        // VÖEN
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? ContractStartAt { get; set; }
    public DateTime? ContractEndAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<Position> Positions { get; set; } = new List<Position>();
}
