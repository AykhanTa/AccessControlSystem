using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>
/// Bir ziyarətin bir cihaza yazılma vəziyyəti. Çoxcihazlı yazma/silmə zamanı
/// hansı cihaza uğurla yazıldığını izləmək (retry + təmizləmə) üçün.
/// </summary>
public class DeviceEnrollment : BaseEntity
{
    public long VisitId { get; set; }
    public Visit Visit { get; set; } = null!;

    public long DeviceId { get; set; }
    public Device Device { get; set; } = null!;

    /// <summary>Cihaza yazılan nömrə (employeeNo = cardNo). Adətən Visit.AccessNumber.</summary>
    public string AccessNumber { get; set; } = string.Empty;

    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Pending;
    public string? LastError { get; set; }
    public DateTime? SyncedAt { get; set; }
    public int Attempts { get; set; }
}
