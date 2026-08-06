namespace AccessControlSystem.Application.DTOs;

/// <summary>Gündəlik davamiyyət hesabatı (işçi × gün).</summary>
public class AttendanceDailyDto
{
    public string FromLabel { get; set; } = "";
    public string ToLabel { get; set; } = "";
    public string Scope { get; set; } = "";
    public string Kind { get; set; } = "";        // "" | late | early | absent | incomplete | overtime | leave | trip
    public string KindLabel { get; set; } = "Bütün günlər";
    public int TotalEmployees { get; set; }
    public int TotalLate { get; set; }
    public int TotalAbsent { get; set; }
    public int TotalOvertimeMin { get; set; }
    public string TotalOvertimeHm { get; set; } = "0";
    public List<AttDayRowDto> Rows { get; set; } = new();
}

public class AttDayRowDto
{
    public string EmployeeNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Department { get; set; }
    public string Date { get; set; } = "";        // dd.MM.yyyy və ya "dd.MM–dd.MM" (aralıq)
    public string Weekday { get; set; } = "";      // B.e, Ç.a … və ya "N gün" (aralıq)
    public int Days { get; set; } = 1;             // aralıqdakı gün sayı
    public string Schedule { get; set; } = "";     // "09:00–18:00" | "İstirahət" | "—"
    public string? In { get; set; }                // HH:mm
    public string? Out { get; set; }               // HH:mm
    public int LateMin { get; set; }
    public int EarlyMin { get; set; }
    public int OvertimeMin { get; set; }
    public int WorkedMin { get; set; }
    public string WorkedHm { get; set; } = "";     // "8s 15d"
    public string OvertimeHm { get; set; } = "";
    public string Status { get; set; } = "";       // normal|late|early|absent|incomplete|rest|noschedule|holiday|leave|trip
    public string StatusLabel { get; set; } = "";
}

/// <summary>İşçi üzrə yekun (aralıq boyu bir sətir) — uzun aralıqda siyahı şişməsin.</summary>
public class AttendanceSummaryDto
{
    public string FromLabel { get; set; } = "";
    public string ToLabel { get; set; } = "";
    public string Scope { get; set; } = "";
    public string KindLabel { get; set; } = "Bütün işçilər";
    public int TotalEmployees { get; set; }
    public List<AttSumRowDto> Rows { get; set; } = new();
}

public class AttSumRowDto
{
    public string EmployeeNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Department { get; set; }
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateDays { get; set; }
    public int LateMin { get; set; }
    public int EarlyDays { get; set; }
    public int IncompleteDays { get; set; }
    public int OvertimeMin { get; set; }
    public int WorkedMin { get; set; }
    public int LeaveDays { get; set; }
    public int TripDays { get; set; }
    public int HolidayDays { get; set; }
    public string WorkedHm { get; set; } = "0";
    public string OvertimeHm { get; set; } = "0";
}

/// <summary>Aylıq kart (Total Time Card) — işçi × ayın günləri grid.</summary>
public class AttendanceMonthlyDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthLabel { get; set; } = "";
    public string Scope { get; set; } = "";
    public List<int> Days { get; set; } = new();   // 1..n
    public List<AttMonthRowDto> Rows { get; set; } = new();
}

public class AttMonthRowDto
{
    public string EmployeeNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Department { get; set; }
    public List<AttCellDto> Cells { get; set; } = new();  // ayın hər günü üçün bir xana
    public int PresentDays { get; set; }
    public int AbsentDays { get; set; }
    public int LateCount { get; set; }
    public int WorkedMin { get; set; }
    public string WorkedHm { get; set; } = "";
}

public class AttCellDto
{
    public int Day { get; set; }
    public string Status { get; set; } = "";   // normal|late|early|absent|incomplete|rest|noschedule
    public string Text { get; set; } = "";     // qısa simvol / saat
    public string? Title { get; set; }         // tooltip (giriş–çıxış, gecikmə)
}
