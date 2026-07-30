using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>
/// Cihazdan gələn real-vaxt keçid hadisəsi (kart/QR oxutma). Hərəkət tarixçəsi
/// və status keçidləri üçün. httpHosts vasitəsilə cihaz serverə göndərir.
/// </summary>
public class AccessEvent : BaseEntity
{
    public long? VisitId { get; set; }
    public Visit? Visit { get; set; }

    public long? EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public long? DeviceId { get; set; }
    public Device? Device { get; set; }

    public string AccessNumber { get; set; } = string.Empty;  // employeeNo/cardNo
    public string? PersonName { get; set; }
    public string EventType { get; set; } = string.Empty;      // qısa etiket (məs. "AccessGranted")
    public bool Granted { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string? DeviceIp { get; set; }
    public string? Raw { get; set; }                            // xam payload (audit/refine üçün)
}
