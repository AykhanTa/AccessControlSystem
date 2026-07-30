namespace AccessControlSystem.Application.DTOs;

/// <summary>
/// Bir Hikvision cihazının şəbəkə ünvanı və Digest krediti.
/// Faza 2-də DB-dəki Device entity-dən doldurulacaq; hazırda test üçün
/// birbaşa ötürülür (məs. appsettings-dən).
/// </summary>
public record HikDevice(
    string Ip,
    string Username,
    string Password,
    int Port = 80,
    bool UseHttps = false)
{
    public string BaseUrl => $"{(UseHttps ? "https" : "http")}://{Ip}:{Port}";

    /// <summary>HttpClient keşi üçün unikal açar (parolsuz).</summary>
    public string Key => $"{(UseHttps ? "https" : "http")}://{Username}@{Ip}:{Port}";
}

/// <summary>
/// Cihazda yaradılacaq/yenilənəcək istifadəçi.
/// Vahid nömrə strategiyası: EmployeeNo = kart nömrəsi = QR mətni = AccessNumber.
/// </summary>
public record HikUser(
    string EmployeeNo,
    string Name,
    DateTime BeginTime,
    DateTime EndTime,
    int DoorNo = 1,
    string PlanTemplateNo = "1");

/// <summary>Cihazdan gələn keçid hadisəsinin parse olunmuş forması.</summary>
public class HikEventDto
{
    public string? AccessNumber { get; set; }   // employeeNo/cardNo
    public string? PersonName { get; set; }
    public string? DeviceIp { get; set; }
    public int? MajorType { get; set; }
    public int? MinorType { get; set; }
    public int? SerialNo { get; set; }   // cihazın hadisə ardıcıllıq nömrəsi (poll dedup üçün)
    public bool Granted { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public DateTimeOffset? DeviceTime { get; set; }   // cihazın bildirdiyi hadisə vaxtı (stale yoxlaması)
    public string? Raw { get; set; }
}

/// <summary>Cihazın AcsEvent jurnalından bir xam sətir (bütün tiplər — qapı, login, üz və s.).</summary>
public class HikRawEvent
{
    public string? EmployeeNo { get; set; }
    public string? Name { get; set; }
    public string? CardNo { get; set; }
    public int? Major { get; set; }
    public int? Minor { get; set; }
    public DateTimeOffset? Time { get; set; }   // cihazın bildirdiyi hadisə vaxtı
    public string? PictureUrl { get; set; }     // cihazdakı snapshot URL-i (varsa)
}

/// <summary>Cihazdan bir səhifə hadisə + ümumi say (server tərəfli pagination üçün).</summary>
public class HikEventRawPage
{
    public bool Ok { get; set; }
    public string? Error { get; set; }
    public int Total { get; set; }               // totalMatches — bütün uyğun hadisələr
    public string? Status { get; set; }          // OK | MORE | NO MATCH
    public List<HikRawEvent> Items { get; set; } = new();
}

/// <summary>Canlı UI yeniləməsi üçün yüngül status cütü.</summary>
public class VisitStatusDto
{
    public long Id { get; set; }
    public string Status { get; set; } = "in";
}

/// <summary>Bir ISAPI əməliyyatının nəticəsi.</summary>
public record HikResult(
    bool Success,
    int HttpStatus,
    int? StatusCode,
    string? SubStatusCode,
    string? ErrorMessage,
    string RawBody)
{
    public static HikResult Fail(string message, int httpStatus = 0, string raw = "") =>
        new(false, httpStatus, null, null, message, raw);

    public override string ToString() =>
        Success
            ? $"OK ({HttpStatus})"
            : $"XƏTA ({HttpStatus}) statusCode={StatusCode} sub={SubStatusCode} msg={ErrorMessage}";
}
