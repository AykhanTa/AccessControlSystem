namespace AccessControlSystem.Application.DTOs;

/// <summary>Sistem loqları cədvəlinin bir sətri.</summary>
public class LogDto
{
    public string DateTime { get; set; } = string.Empty;   // "8 iyl 2026, 10:46"
    public string Action { get; set; } = string.Empty;     // ROLE_PERMISSION_UPDATED
    public string Content { get; set; } = string.Empty;    // təsvir
    public string PerformedBy { get; set; } = "Sistem";    // İcra edən
}

/// <summary>Səhifələnmiş loq nəticəsi.</summary>
public class PagedLogsDto
{
    public List<LogDto> Items { get; set; } = new();
    public int Total { get; set; }        // filtrə uyğun ümumi say
    public int Page { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalPages { get; set; } = 1;
}
