using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Şöbə — şirkət daxilində struktur vahidi (iyerarxik ola bilər).</summary>
public class Department : BaseEntity
{
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public long? ParentDepartmentId { get; set; }         // ana şöbə (iyerarxiya)
    public Department? ParentDepartment { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();

    public bool IsActive { get; set; } = true;
}
