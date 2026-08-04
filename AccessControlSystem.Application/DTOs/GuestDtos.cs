namespace AccessControlSystem.Application.DTOs;

/// <summary>Qonaq reyestri / "Son qonaqlar" cədvəlinin bir sətri (ziyarət əsaslı).</summary>
public class VisitRowDto
{
    public long Id { get; set; }                 // ziyarət id
    public string Name { get; set; } = string.Empty;
    public string Doc { get; set; } = string.Empty;
    public string? Photo { get; set; }
    public string Host { get; set; } = string.Empty;
    public string Arrival { get; set; } = string.Empty;   // planlaşdırılan gəliş
    public string? Entry { get; set; }                    // faktiki binaya giriş vaxtı (varsa)
    public string? Exit { get; set; }
    public string Area { get; set; } = string.Empty;      // vergüllə birləşdirilmiş (köhnə)
    public string Floor { get; set; } = string.Empty;     // icazəli mərtəbələr (yeni)
    public string Purpose { get; set; } = string.Empty;   // vergüllə birləşdirilmiş
    public string Status { get; set; } = "in";            // "in" | "out" | "late"
    public string PassType { get; set; } = "card";        // "card" | "qr"
    public string? CardNo { get; set; }
    public string? AccessNumber { get; set; }             // QR/kart üçün cihaz nömrəsi
    public long? CompanyId { get; set; }                  // gəldiyi müəssisə (təhlükəsizlik nəzarəti üçün)
    public string? Company { get; set; }                  // müəssisə adı (controller-də həll olunur)
}

/// <summary>Yeni qonaq + ziyarət qeydiyyatı üçün gələn məlumat.</summary>
public class GuestCreateDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string IdDocument { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoPath { get; set; }
    public string? DocumentPath { get; set; }

    public long HostId { get; set; }
    /// <summary>Qonağın gəldiyi şirkət (qlobal admin seçir; şirkət istifadəçisində öz şirkəti tətbiq olunur).</summary>
    public long? CompanyId { get; set; }
    public DateTime ArrivalAt { get; set; }
    public DateTime? ExpectedExitAt { get; set; }
    public string PassType { get; set; } = "card";   // "card" | "qr"
    public long? CardId { get; set; }                // PassType="card" olduqda
    public List<long> FloorIds { get; set; } = new();  // icazəli mərtəbələr → cihazlara yazılır
    public List<long> AreaIds { get; set; } = new();
    public List<long> PurposeIds { get; set; } = new();
    public string? Note { get; set; }
}
