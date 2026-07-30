namespace AccessControlSystem.Application.DTOs;

/// <summary>Keçid hadisələri cədvəlinin bir sətri (qonaq + işçi, real-vaxt).</summary>
public class AccessEventRowDto
{
    public long Id { get; set; }
    public string Time { get; set; } = string.Empty;        // dd.MM.yyyy HH:mm:ss
    public string AccessNumber { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string PersonType { get; set; } = "";            // "guest" | "employee" | ""
    public bool Granted { get; set; }
    public string? Point { get; set; }                      // keçid nöqtəsi / cihaz adı
    public string? Direction { get; set; }                  // "entry" | "exit"
}

/// <summary>Cihaz jurnalı (Hikvision Event Search) cədvəlinin bir sətri.</summary>
public class HikDeviceEventRow
{
    public int No { get; set; }
    public string EmployeeId { get; set; } = "--";
    public string Name { get; set; } = "-";
    public string CardNo { get; set; } = "--";
    public string EventType { get; set; } = "";
    public string Time { get; set; } = "";                  // dd.MM.yyyy HH:mm:ss
    public string? PhotoUrl { get; set; }                   // proxy URL (varsa) — cihaz snapshot-u
}

/// <summary>Cihaz jurnalının bir səhifəsi + pagination məlumatı.</summary>
public class HikDeviceEventPageDto
{
    public List<HikDeviceEventRow> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public string? Error { get; set; }
}
