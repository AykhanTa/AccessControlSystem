using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>İşçinin məzuniyyət/ezamiyyət/xəstəlik qeydi (tarix aralığı).
/// HR birbaşa yaradır — davamiyyət hesablanmasında nəzərə alınır (üzrlü / işdə sayılır).</summary>
public class LeaveRecord : BaseEntity
{
    public long CompanyId { get; set; }              // işçinin şirkəti (təcrid üçün)

    public long EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public long LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; }

    public DateTime StartDate { get; set; }          // daxil (00:00)
    public DateTime EndDate { get; set; }            // daxil (o günün sonuna qədər)

    public string? Reason { get; set; }
    public long? CreatedByUserId { get; set; }       // qeyd edən HR/admin
}
