namespace AccessControlSystem.Application.DTOs;

/// <summary>İş cədvəlinin (timetable) siyahı sətri (Ayarlar səhifəsi üçün).</summary>
public class WorkScheduleItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long? CompanyId { get; set; }
    public string CompanyName { get; set; } = "Qlobal";
    public string Type { get; set; } = "Normal";       // Normal | Flexible
    public string StartTime { get; set; } = "09:00";    // HH:mm
    public string EndTime { get; set; } = "18:00";      // HH:mm
    public int GraceMinutes { get; set; }
    public int EarlyLeaveGraceMinutes { get; set; }
    public string? CheckInStart { get; set; }           // HH:mm | null
    public string? CheckInEnd { get; set; }
    public string? CheckOutStart { get; set; }
    public string? CheckOutEnd { get; set; }
    public int? AbsentAfterMinutes { get; set; }
    public int MinWorkMinutes { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public string DaysLabel { get; set; } = "";         // məs. "B.e, Ç.a, Ç, C.a, C"
    public int UsageCount { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Yeni/redaktə iş cədvəli formu.</summary>
public class WorkScheduleInputDto
{
    public string Name { get; set; } = "";
    public long? CompanyId { get; set; }
    public string Type { get; set; } = "Normal";        // Normal | Flexible
    public string StartTime { get; set; } = "09:00";    // HH:mm
    public string EndTime { get; set; } = "18:00";      // HH:mm
    public int GraceMinutes { get; set; }
    public int EarlyLeaveGraceMinutes { get; set; }
    public string? CheckInStart { get; set; }           // HH:mm | boş = məhdudiyyət yoxdur
    public string? CheckInEnd { get; set; }
    public string? CheckOutStart { get; set; }
    public string? CheckOutEnd { get; set; }
    public int? AbsentAfterMinutes { get; set; }
    public int MinWorkMinutes { get; set; }
    public string? Color { get; set; }
    public bool Mon { get; set; } = true;
    public bool Tue { get; set; } = true;
    public bool Wed { get; set; } = true;
    public bool Thu { get; set; } = true;
    public bool Fri { get; set; } = true;
    public bool Sat { get; set; }
    public bool Sun { get; set; }
}
