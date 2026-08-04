using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Daimi işçi — üz/kartla keçid edən. Şirkət/şöbə/vəzifəyə aiddir.</summary>
public class Employee : BaseEntity
{
    public long CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public long? PositionId { get; set; }
    public Position? Position { get; set; }

    public string EmployeeNo { get; set; } = string.Empty;   // unikal tabel nömrəsi
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string? FinCode { get; set; }
    public string? DocumentNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoPath { get; set; }

    public DateTime? EmploymentStartAt { get; set; }
    public DateTime? EmploymentEndAt { get; set; }

    public EmployeeStatus Status { get; set; } = EmployeeStatus.Active;
    public FaceStatus FaceStatus { get; set; } = FaceStatus.None;

    /// <summary>Cihaza yazılan vahid nömrə (employeeNo=cardNo). Enroll zamanı təyin olunur.</summary>
    public string? AccessNumber { get; set; }

    /// <summary>Cihazlardakı əlavə istifadəçi ID-ləri (alias) — "cihazId:nömrə" (məs. "2:54,3:29").
    /// İşçinin müxtəlif cihazlarda fərqli ID-si olduqda keçidlərin doğru uyğunlaşması üçün (override).</summary>
    public string? DeviceNumbers { get; set; }

    /// <summary>Hikvision cihazlarında göründüyü ad (məs. "Alijavad Sadikhzada"). Keçidləri
    /// dəqiq uyğunlaşdırmaq üçün ƏSAS açar — cihaz hər hadisə ilə bu adı göndərir.</summary>
    public string? DeviceName { get; set; }

    /// <summary>Hazırkı mövqe — keçid hadisələrindən yenilənir.</summary>
    public PresenceStatus CurrentPresence { get; set; } = PresenceStatus.Out;
    public DateTime? LastSeenAt { get; set; }

    public string FullName => $"{LastName} {FirstName}".Trim();

    public ICollection<EmployeeFloor> EmployeeFloors { get; set; } = new List<EmployeeFloor>();
}
