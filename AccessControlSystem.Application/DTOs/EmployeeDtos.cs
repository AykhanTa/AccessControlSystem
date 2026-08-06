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
    public long? WorkScheduleId { get; set; }            // effektiv cədvəl id (fərdi və ya paylaşılan)
    public string? WorkScheduleName { get; set; }        // effektiv cədvəlin adı (şöbədən miras da ola bilər)
    public bool ScheduleFromDept { get; set; }           // cədvəl şöbədən miras alınıb?

    // İş cədvəli rejimi (redaktə formunu doldurmaq üçün)
    public string ScheduleChoice { get; set; } = "";     // "" = miras | "custom" = fərdi | "<id>" = paylaşılan
    public string SchedStart { get; set; } = "09:00";
    public string SchedEnd { get; set; } = "18:00";
    public int SchedGrace { get; set; }
    public int SchedEarly { get; set; }
    public bool SMon { get; set; } = true;
    public bool STue { get; set; } = true;
    public bool SWed { get; set; } = true;
    public bool SThu { get; set; } = true;
    public bool SFri { get; set; } = true;
    public bool SSat { get; set; }
    public bool SSun { get; set; }

    public string? EmploymentStartAt { get; set; }       // yyyy-MM-dd
    public string? DeviceNumbers { get; set; }           // cihaz alias-ları (cihazId:nömrə)
    public string? DeviceName { get; set; }              // Hikvision-dakı ad
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

    // İş cədvəli seçimi
    public string ScheduleChoice { get; set; } = "";     // "" = şöbədən miras | "custom" = fərdi | "<id>" = paylaşılan
    public string? CustomStart { get; set; }             // fərdi: HH:mm
    public string? CustomEnd { get; set; }
    public int CustomGrace { get; set; }
    public int CustomEarly { get; set; }
    public bool SMon { get; set; } = true;
    public bool STue { get; set; } = true;
    public bool SWed { get; set; } = true;
    public bool SThu { get; set; } = true;
    public bool SFri { get; set; } = true;
    public bool SSat { get; set; }
    public bool SSun { get; set; }

    public DateTime? EmploymentStartAt { get; set; }
    public string? DeviceNumbers { get; set; }           // cihaz alias-ları ("cihazId:nömrə")
    public string? DeviceName { get; set; }              // Hikvision-dakı ad (əsas uyğunlaşma açarı)

    public List<long> FloorIds { get; set; } = new();
}
