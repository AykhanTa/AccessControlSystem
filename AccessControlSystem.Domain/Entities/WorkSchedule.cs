using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>İş cədvəli (Timetable) — bir günün davamiyyət qaydası: iş saatları, günlər,
/// keçərli giriş/çıxış pəncərələri, tip. Gecikmə / erkən çıxış / qayıb / işlənmiş saat
/// hesabatları buna əsaslanır. Bax: docs/ATTENDANCE_DESIGN.md</summary>
public class WorkSchedule : BaseEntity
{
    /// <summary>Sahibi şirkət (null = qlobal, bütün şirkətlərə uyğun).</summary>
    public long? CompanyId { get; set; }
    public Company? Company { get; set; }

    /// <summary>Fərdi cədvəl — bu işçiyə xüsusidir (işçi formundan yaradılır).
    /// null = paylaşılan cədvəl (İş cədvəlləri səhifəsində görünür). Dəyər varsa
    /// yalnız həmin işçiyə aiddir, paylaşılan siyahılarda gizlədilir.</summary>
    public long? OwnerEmployeeId { get; set; }

    public string Name { get; set; } = string.Empty;    // məs. "Standart 09:00–18:00"

    /// <summary>Normal (sabit) və ya Flexible (çevik — yalnız işlənmiş vaxt sayılır).</summary>
    public TimetableType Type { get; set; } = TimetableType.Normal;

    public TimeSpan StartTime { get; set; } = new(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new(18, 0, 0);

    /// <summary>İcazəli gecikmə (dəqiqə) — bu qədər gec gəlmək gecikmə sayılmır.</summary>
    public int GraceMinutes { get; set; } = 0;

    /// <summary>İcazəli erkən çıxış (dəqiqə).</summary>
    public int EarlyLeaveGraceMinutes { get; set; } = 0;

    // --- Keçərli oxutma pəncərələri (opsional; null = məhdudiyyət yoxdur) ---
    /// <summary>Girişin qəbul olunduğu ən erkən vaxt (məs. 07:00).</summary>
    public TimeSpan? CheckInStart { get; set; }
    /// <summary>Girişin qəbul olunduğu ən gec vaxt (məs. 11:00).</summary>
    public TimeSpan? CheckInEnd { get; set; }
    /// <summary>Çıxışın qəbul olunduğu ən erkən vaxt (məs. 16:00).</summary>
    public TimeSpan? CheckOutStart { get; set; }
    /// <summary>Çıxışın qəbul olunduğu ən gec vaxt (məs. 20:00).</summary>
    public TimeSpan? CheckOutEnd { get; set; }

    /// <summary>Giriş bu qədər dəqiqədən çox gecikərsə → qayıb sayılır (null = qayda default-u).</summary>
    public int? AbsentAfterMinutes { get; set; }

    /// <summary>Flexible tip üçün minimum işlənmə (dəqiqə) — bundan az işləmək natamam sayılır.</summary>
    public int MinWorkMinutes { get; set; } = 0;

    /// <summary>Təqvim/UI rəngi (hex, məs. "#3b82f6").</summary>
    public string? Color { get; set; }

    // İş günləri (default: B.e–Cümə)
    public bool Mon { get; set; } = true;
    public bool Tue { get; set; } = true;
    public bool Wed { get; set; } = true;
    public bool Thu { get; set; } = true;
    public bool Fri { get; set; } = true;
    public bool Sat { get; set; }
    public bool Sun { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Verilən günün iş günü olub-olmadığını qaytarır.</summary>
    public bool IsWorkDay(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => Mon,
        DayOfWeek.Tuesday => Tue,
        DayOfWeek.Wednesday => Wed,
        DayOfWeek.Thursday => Thu,
        DayOfWeek.Friday => Fri,
        DayOfWeek.Saturday => Sat,
        DayOfWeek.Sunday => Sun,
        _ => false
    };
}
