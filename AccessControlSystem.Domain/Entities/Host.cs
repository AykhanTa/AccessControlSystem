using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Qəbul edən şəxs.</summary>
public class Host : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }     // Şöbə/vəzifə
    public bool IsActive { get; set; } = true;

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
