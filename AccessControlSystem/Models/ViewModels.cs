using AccessControlSystem.Application.DTOs;

namespace AccessControlSystem.Models;

/// <summary>Ana səhifə (dashboard) view modeli.</summary>
public class DashboardViewModel
{
    public DashboardStatsDto Stats { get; set; } = new();
    public List<VisitRowDto> RecentGuests { get; set; } = new();
}

/// <summary>Qonaqlar səhifəsi view modeli.</summary>
public class GuestsViewModel
{
    public List<VisitRowDto> Guests { get; set; } = new();
    public List<LookupDto> Hosts { get; set; } = new();
    public List<LookupDto> Areas { get; set; } = new();
    public List<LookupDto> Floors { get; set; } = new();
    public List<LookupDto> Purposes { get; set; } = new();
    public List<LookupDto> FreeCards { get; set; } = new();
}

/// <summary>İşçilər səhifəsi view modeli.</summary>
public class EmployeesViewModel
{
    public List<EmployeeRowDto> Employees { get; set; } = new();
    public List<CompanyItemDto> Companies { get; set; } = new();
    public List<DepartmentItemDto> Departments { get; set; } = new();
    public List<PositionItemDto> Positions { get; set; } = new();
    public List<LookupDto> Floors { get; set; } = new();
}

/// <summary>İşçi əlavə/redaktə formu (binding üçün).</summary>
public class EmployeeFormModel
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string? EmployeeNo { get; set; }
    public string? FinCode { get; set; }
    public string? DocumentNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public long CompanyId { get; set; }
    public long? DepartmentId { get; set; }
    public long? PositionId { get; set; }
    public DateTime? EmploymentStartAt { get; set; }
    public List<long> FloorIds { get; set; } = new();

    public IFormFile? Photo { get; set; }
}

/// <summary>Kartlar səhifəsi view modeli.</summary>
public class CardsViewModel
{
    public List<CardDto> Cards { get; set; } = new();
}

/// <summary>Aktiv icazələr səhifəsi view modeli.</summary>
public class ActivePermitsViewModel
{
    public List<VisitRowDto> Permits { get; set; } = new();
    public List<LookupDto> Hosts { get; set; } = new();
    public List<LookupDto> Areas { get; set; } = new();
    public List<LookupDto> Purposes { get; set; } = new();
}

/// <summary>Giriş-çıxış tarixçəsi səhifəsi view modeli.</summary>
public class HistoryViewModel
{
    public List<VisitRowDto> Items { get; set; } = new();
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
}

/// <summary>Hesabatlar səhifəsi view modeli.</summary>
public class ReportsViewModel
{
    public ReportDto Report { get; set; } = new();
    public List<int> Years { get; set; } = new();
    public int SelectedYear { get; set; }
}

/// <summary>İstifadəçilər səhifəsi view modeli.</summary>
public class UsersViewModel
{
    public List<UserDto> Users { get; set; } = new();
    public List<LookupDto> Roles { get; set; } = new();
    public List<CompanyItemDto> Companies { get; set; } = new();
}

/// <summary>Rollar səhifəsi view modeli.</summary>
public class RolesViewModel
{
    public List<RoleDto> Roles { get; set; } = new();
}

/// <summary>Parametrlər səhifəsi view modeli.</summary>
public class SettingsViewModel
{
    public List<HostItemDto> Hosts { get; set; } = new();
    public List<AreaItemDto> Areas { get; set; } = new();
    public List<PurposeItemDto> Purposes { get; set; } = new();
    public List<CompanyItemDto> Companies { get; set; } = new();
    public List<DepartmentItemDto> Departments { get; set; } = new();
    public List<PositionItemDto> Positions { get; set; } = new();
    public List<CenterItemDto> Centers { get; set; } = new();
    public List<FloorItemDto> Floors { get; set; } = new();
    public List<DeviceItemDto> Devices { get; set; } = new();
}

/// <summary>Sistem Loqları səhifəsi view modeli.</summary>
public class LogsViewModel
{
    public List<LogDto> Logs { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalPages { get; set; } = 1;
    public string? Query { get; set; }
    public bool PrintMode { get; set; }
}

/// <summary>Login səhifəsi view modeli.</summary>
public class LoginViewModel
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}

/// <summary>Yeni qonaq modalının formu (binding üçün).</summary>
public class GuestFormModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Patronymic { get; set; }
    public string IdDocument { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public long HostId { get; set; }
    public DateTime ArrivalDate { get; set; }
    public string ArrivalTime { get; set; } = "09:00";
    public DateTime? ExitDate { get; set; }
    public string? ExitTime { get; set; }

    public string PassType { get; set; } = "card";   // "card" | "qr"
    public long? CardId { get; set; }
    public long FloorId { get; set; }                  // icazəli mərtəbə
    public long PurposeId { get; set; }
    public string? Note { get; set; }

    public IFormFile? Photo { get; set; }
    public IFormFile? Document { get; set; }
}
