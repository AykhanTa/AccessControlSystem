namespace AccessControlSystem.Application.DTOs;

/// <summary>İşçi siyahısının bir sətri.</summary>
public class EmployeeRowDto
{
    public long Id { get; set; }
    public string EmployeeNo { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Photo { get; set; }
    public string Company { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Phone { get; set; }
    public string Floors { get; set; } = string.Empty;
    public string Status { get; set; } = "active";       // "active" | "inactive" | "terminated"
    public string FaceStatus { get; set; } = "none";     // "none" | "pending" | "synced" | "failed"
    public string Presence { get; set; } = "out";        // "out" | "in" | "onfloor"
    public string? LastSeen { get; set; }

    // Redaktə formunun doldurulması üçün
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string? FinCode { get; set; }
    public string? DocumentNo { get; set; }
    public string? Email { get; set; }
    public long CompanyId { get; set; }
    public long? DepartmentId { get; set; }
    public long? PositionId { get; set; }
    public string? EmploymentStartAt { get; set; }       // yyyy-MM-dd
    public List<long> FloorIds { get; set; } = new();
}

/// <summary>Canlı UI üçün işçi mövqe cütü.</summary>
public class EmployeePresenceDto
{
    public long Id { get; set; }
    public string Presence { get; set; } = "out";
    public string? LastSeen { get; set; }
}

/// <summary>Yeni/redaktə işçi formu.</summary>
public class EmployeeCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string? EmployeeNo { get; set; }              // boş olsa avtomatik generasiya
    public string? FinCode { get; set; }
    public string? DocumentNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoPath { get; set; }

    public long CompanyId { get; set; }
    public long? DepartmentId { get; set; }
    public long? PositionId { get; set; }
    public DateTime? EmploymentStartAt { get; set; }

    public List<long> FloorIds { get; set; } = new();
}
