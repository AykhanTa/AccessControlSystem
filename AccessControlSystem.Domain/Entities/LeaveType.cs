using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Məzuniyyət/ezamiyyət növü. HR işçilər üçün qeyd yaradanda seçilir.
/// <see cref="CountsAsWorked"/> = true olduqda (məs. ezamiyyət) həmin günlər işdə sayılır,
/// qayıb kimi hesablanmır; false olduqda (məzuniyyət, xəstəlik) üzrlü sayılır.</summary>
public class LeaveType : BaseEntity
{
    /// <summary>Sahibi şirkət (null = qlobal).</summary>
    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    public string Name { get; set; } = string.Empty;   // "İllik məzuniyyət", "Ezamiyyət"

    /// <summary>İşdə sayılırmı? Ezamiyyət=true, məzuniyyət/xəstəlik=false.</summary>
    public bool CountsAsWorked { get; set; }

    public bool Paid { get; set; } = true;
    public string? Color { get; set; }
    public bool IsActive { get; set; } = true;
}
