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

    /// <summary>Şöbənin standart iş cədvəli — işçidə override yoxdursa buradan götürülür.</summary>
    public long? WorkScheduleId { get; set; }
    public WorkSchedule? WorkSchedule { get; set; }

    public bool IsActive { get; set; } = true;
}
