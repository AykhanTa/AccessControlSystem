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
    public string DayStart { get; set; } = "05:00";     // gün başlanğıcı (oxutma sərhədi) HH:mm
    public int? AbsentAfterMinutes { get; set; }
    public int MinWorkMinutes { get; set; }
    public string Color { get; set; } = "#3b82f6";
    public string DaysLabel { get; set; } = "";         // məs. "B.e, Ç.a, Ç, C.a, C"
    // Redaktə formunda günləri bərpa etmək üçün.
    public bool Mon { get; set; }
    public bool Tue { get; set; }
    public bool Wed { get; set; }
    public bool Thu { get; set; }
    public bool Fri { get; set; }
    public bool Sat { get; set; }
    public bool Sun { get; set; }
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
    public string DayStart { get; set; } = "05:00";     // gün başlanğıcı (oxutma sərhədi) HH:mm
    public int? AbsentAfterMinutes { get; set; }
    public int MinWorkMinutes { get; set; }
    public string? Color { get; set; }
    // Default FALSE — işarəsiz checkbox göndərilmir, binder default-u saxlayır.
    // (true default olsaydı seçilməyən günlər də true qalardı — 5 gün bug-ı.)
    public bool Mon { get; set; }
    public bool Tue { get; set; }
    public bool Wed { get; set; }
    public bool Thu { get; set; }
    public bool Fri { get; set; }
    public bool Sat { get; set; }
    public bool Sun { get; set; }
}
