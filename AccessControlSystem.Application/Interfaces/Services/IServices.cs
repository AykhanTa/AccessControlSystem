using AccessControlSystem.Application.DTOs;

namespace AccessControlSystem.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default);
    Task<List<VisitRowDto>> GetRecentGuestsAsync(int count = 10, CancellationToken ct = default);
}

public interface IGuestService
{
    Task<List<VisitRowDto>> GetRegistryAsync(CancellationToken ct = default);
    Task<long> RegisterAsync(GuestCreateDto dto, CancellationToken ct = default);
    /// <summary>Ziyarətin çıxışını təsdiqləyir (status "out"). Kart varsa boşaldılır.</summary>
    Task CheckOutAsync(long visitId, CancellationToken ct = default);
}

public interface ICardService
{
    Task<List<CardDto>> GetAllAsync(CancellationToken ct = default);
    Task<long> CreateAsync(CardCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, CardUpdateDto dto, CancellationToken ct = default);
    Task ToggleActiveAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IActivePermitService
{
    /// <summary>Hazırda binada olan (aktiv icazəli) qonaqlar.</summary>
    Task<List<VisitRowDto>> GetActiveAsync(CancellationToken ct = default);
}

public interface IHistoryService
{
    /// <summary>Giriş-çıxış tarixçəsi (tarix aralığı ilə filtrlənə bilər).</summary>
    Task<List<VisitRowDto>> GetHistoryAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
}

public interface IReportService
{
    /// <summary>Ziyarətlərin mövcud olduğu illər.</summary>
    Task<List<int>> GetYearsAsync(CancellationToken ct = default);
    /// <summary>Verilmiş il üzrə hesabat göstəriciləri.</summary>
    Task<ReportDto> GetReportAsync(int year, CancellationToken ct = default);
}

public interface IUserService
{
    Task<List<UserDto>> GetAllAsync(CancellationToken ct = default);
    Task<long> CreateAsync(UserCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, UserUpdateDto dto, CancellationToken ct = default);
    Task ToggleStatusAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IRoleService
{
    Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default);
    /// <summary>İstifadəçi formu üçün rol siyahısı (id + ad).</summary>
    Task<List<LookupDto>> GetRoleOptionsAsync(CancellationToken ct = default);
    Task<long> CreateAsync(RoleCreateDto dto, CancellationToken ct = default);
    /// <summary>Rolun bütün bölmə icazələrini yeniləyir.</summary>
    Task UpdatePermissionsAsync(long roleId, List<SectionPermDto> permissions, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ISystemLogWriter
{
    /// <summary>Audit loqu yazır. actorName verilməsə cari istifadəçi götürülür.</summary>
    Task LogAsync(string action, string description, string? entityType = null, long? entityId = null,
                  long? actorUserId = null, string? actorName = null, CancellationToken ct = default);
}

public interface ISystemLogService
{
    /// <summary>Axtarış + səhifələmə ilə loqlar.</summary>
    Task<PagedLogsDto> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct = default);
}

public interface IAuthService
{
    /// <summary>E-poçt və şifrəni sistemdəki istifadəçilərə əsasən yoxlayır.</summary>
    Task<AuthResultDto> AuthenticateAsync(string email, string password, CancellationToken ct = default);
    /// <summary>Rolun bütün bölmələr üzrə icazə xəritəsi (kod → view/add/edit/delete).</summary>
    Task<Dictionary<string, SectionAccessDto>> GetPermissionMapAsync(long roleId, CancellationToken ct = default);
}

public interface ISettingsService
{
    // Qəbul edən şəxslər
    Task<List<HostItemDto>> GetHostsAsync(CancellationToken ct = default);
    Task<long> AddHostAsync(HostInputDto dto, CancellationToken ct = default);
    Task UpdateHostAsync(long id, HostInputDto dto, CancellationToken ct = default);
    Task ToggleHostAsync(long id, CancellationToken ct = default);
    Task DeleteHostAsync(long id, CancellationToken ct = default);

    // Giriş əraziləri
    Task<List<AreaItemDto>> GetAreasAsync(CancellationToken ct = default);
    Task<long> AddAreaAsync(string name, CancellationToken ct = default);
    Task DeleteAreaAsync(long id, CancellationToken ct = default);

    // Gəliş məqsədləri
    Task<List<PurposeItemDto>> GetPurposesAsync(CancellationToken ct = default);
    Task<long> AddPurposeAsync(string name, CancellationToken ct = default);
    Task TogglePurposeAsync(long id, CancellationToken ct = default);
}

public interface ILookupService
{
    Task<List<LookupDto>> GetHostsAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetAreasAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetPurposesAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetFreeCardsAsync(CancellationToken ct = default);
}
