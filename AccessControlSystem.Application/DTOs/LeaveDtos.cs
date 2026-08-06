namespace AccessControlSystem.Application.DTOs;

// ---------- Növlər ----------
public class LeaveTypeItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool CountsAsWorked { get; set; }
    public bool Paid { get; set; }
    public string Color { get; set; } = "#8b5cf6";
    public string CompanyName { get; set; } = "Qlobal";
    public int UsageCount { get; set; }
    public bool IsActive { get; set; } = true;
}

public class LeaveTypeInputDto
{
    public string Name { get; set; } = "";
    public bool CountsAsWorked { get; set; }
    public bool Paid { get; set; } = true;
    public string? Color { get; set; }
    public long? CompanyId { get; set; }
}

// ---------- Qeydlər ----------
public class LeaveRecordItemDto
{
    public long Id { get; set; }
    public string EmployeeName { get; set; } = "";
    public string EmployeeNo { get; set; } = "";
    public string? Department { get; set; }
    public string TypeName { get; set; } = "";
    public bool CountsAsWorked { get; set; }
    public string Color { get; set; } = "#8b5cf6";
    public string StartDate { get; set; } = "";   // dd.MM.yyyy
    public string EndDate { get; set; } = "";
    public int Days { get; set; }
    public string? Reason { get; set; }
}

public class LeaveRecordInputDto
{
    public long EmployeeId { get; set; }
    public long LeaveTypeId { get; set; }
    public string StartDate { get; set; } = "";    // yyyy-MM-dd
    public string EndDate { get; set; } = "";
    public string? Reason { get; set; }
}

// ---------- Bayramlar ----------
public class HolidayItemDto
{
    public string Ids { get; set; } = "";           // aralıqdakı bütün gün id-ləri (csv) — silmə üçün
    public string RangeLabel { get; set; } = "";    // "20.03.2026 – 31.03.2026" və ya tək gün
    public int Days { get; set; }
    public string Name { get; set; } = "";
    public string CompanyName { get; set; } = "Qlobal";
}

public class HolidayInputDto
{
    public string StartDate { get; set; } = "";     // yyyy-MM-dd
    public string EndDate { get; set; } = "";       // yyyy-MM-dd (aralıq; tək gün üçün eyni)
    public string Name { get; set; } = "";
    public long? CompanyId { get; set; }
}
