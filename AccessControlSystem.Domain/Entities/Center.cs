using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Mərkəz — bina/obyekt. Mərtəbələr və keçid nöqtələri ona bağlanır (çoxbina dəstəyi).</summary>
public class Center : BaseEntity
{
    public string Code { get; set; } = string.Empty;      // qısa kod, məs. "HQ"
    public string Name { get; set; } = string.Empty;      // məs. "Baş Ofis"
    public string? Address { get; set; }
    public string? City { get; set; }
    public TimeSpan? WorkingHoursStart { get; set; }
    public TimeSpan? WorkingHoursEnd { get; set; }
    public string? TimeZone { get; set; }                 // məs. "CST-4:00:00" (Bakı UTC+4)
    public bool IsActive { get; set; } = true;

    /// <summary>Sahibi olan şirkət (çoxkiracılı təcrid). null = qlobal/təyin edilməyib.</summary>
    public long? CompanyId { get; set; }

    public ICollection<Floor> Floors { get; set; } = new List<Floor>();
}
