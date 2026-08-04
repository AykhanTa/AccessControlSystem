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
    /// <summary>Canlı UI üçün — id + status cütləri.</summary>
    Task<List<VisitStatusDto>> GetStatusesAsync(CancellationToken ct = default);
    /// <summary>Əvvəlcədən qeydiyyat — status "planlaşdırılmış". Kart check-in-də təyin olunur.</summary>
    Task<long> RegisterAsync(GuestCreateDto dto, CancellationToken ct = default);
    /// <summary>Nəzarətçi check-in: boş kart təyin edir, cihazlara yazır, status "kart verilib".</summary>
    Task CheckInAsync(long visitId, long? cardId, CancellationToken ct = default);
    /// <summary>Ziyarətin çıxışını təsdiqləyir (status "out"). Kart varsa boşaldılır.</summary>
    Task CheckOutAsync(long visitId, CancellationToken ct = default);
}

public interface IAccessEventService
{
    /// <summary>Verilmiş günün bütün keçid hadisələri (day null → bugün).</summary>
    Task<List<AccessEventRowDto>> GetByDayAsync(DateTime? day = null, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<List<EmployeeRowDto>> GetAllAsync(CancellationToken ct = default);
    /// <summary>Canlı UI üçün — id + mövqe cütləri.</summary>
    Task<List<EmployeePresenceDto>> GetPresencesAsync(CancellationToken ct = default);
    Task<long> CreateAsync(EmployeeCreateDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, EmployeeCreateDto dto, CancellationToken ct = default);
    Task ToggleStatusAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
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
    /// <summary>Müəssisə + şöbə üzrə işçi giriş-çıxış hesabatı (null → bütün müəssisələr/şöbələr).</summary>
    Task<DeptAccessReportDto> GetDepartmentAccessAsync(long? companyId, long? departmentId, DateTime from, DateTime to, CancellationToken ct = default);
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
    Task DeletePurposeAsync(long id, CancellationToken ct = default);

    // Şirkətlər
    Task<List<CompanyItemDto>> GetCompaniesAsync(CancellationToken ct = default);
    Task<long> AddCompanyAsync(CompanyInputDto dto, CancellationToken ct = default);
    Task ToggleCompanyAsync(long id, CancellationToken ct = default);
    Task DeleteCompanyAsync(long id, CancellationToken ct = default);

    // Şöbələr
    Task<List<DepartmentItemDto>> GetDepartmentsAsync(CancellationToken ct = default);
    Task<long> AddDepartmentAsync(string name, long companyId, long? parentId, CancellationToken ct = default);
    Task ToggleDepartmentAsync(long id, CancellationToken ct = default);
    Task DeleteDepartmentAsync(long id, CancellationToken ct = default);

    // Vəzifələr
    Task<List<PositionItemDto>> GetPositionsAsync(CancellationToken ct = default);
    Task<long> AddPositionAsync(string name, long companyId, CancellationToken ct = default);
    Task TogglePositionAsync(long id, CancellationToken ct = default);
    Task DeletePositionAsync(long id, CancellationToken ct = default);

    // Mərkəzlər (binalar)
    Task<List<CenterItemDto>> GetCentersAsync(CancellationToken ct = default);
    Task<long> AddCenterAsync(CenterInputDto dto, CancellationToken ct = default);
    Task UpdateCenterAsync(long id, CenterInputDto dto, CancellationToken ct = default);
    Task ToggleCenterAsync(long id, CancellationToken ct = default);
    Task DeleteCenterAsync(long id, CancellationToken ct = default);

    // Mərtəbələr
    Task<List<FloorItemDto>> GetFloorsAsync(CancellationToken ct = default);
    Task<long> AddFloorAsync(string name, long? centerId, CancellationToken ct = default);
    Task ToggleFloorAsync(long id, CancellationToken ct = default);
    Task DeleteFloorAsync(long id, CancellationToken ct = default);

    // Cihazlar
    Task<List<DeviceItemDto>> GetDevicesAsync(CancellationToken ct = default);
    Task<long> AddDeviceAsync(DeviceInputDto dto, CancellationToken ct = default);
    Task UpdateDeviceAsync(long id, DeviceInputDto dto, CancellationToken ct = default);
    Task ToggleDeviceAsync(long id, CancellationToken ct = default);
    Task DeleteDeviceAsync(long id, CancellationToken ct = default);
}

public interface ILookupService
{
    Task<List<LookupDto>> GetHostsAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetAreasAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetPurposesAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetFreeCardsAsync(CancellationToken ct = default);
    Task<List<LookupDto>> GetFloorsAsync(CancellationToken ct = default);
}

/// <summary>
/// Bir ziyarəti fiziki Hikvision cihazlarına yazan/silən orchestration servisi.
/// Cihaz xətaları qeydiyyatı bloklamır — hər cihazın nəticəsi DeviceEnrollment-də saxlanır.
/// </summary>
public interface IVisitAccessService
{
    /// <summary>Ziyarətin icazəli mərtəbələrinin bütün aktiv cihazlarına AccessNumber yazır.
    /// Uğurla yazılan cihaz sayını qaytarır.</summary>
    Task<int> EnrollAsync(long visitId, string accessNumber, string guestName,
        DateTime begin, DateTime end, IEnumerable<long> floorIds, CancellationToken ct = default);

    /// <summary>Ziyarətin bütün cihaz-qeydlərini cihazlardan silir.</summary>
    Task RevokeAsync(long visitId, CancellationToken ct = default);
}

/// <summary>Cihazdan gələn keçid hadisələrini emal edir və status keçidlərini icra edir.</summary>
public interface IVisitEventService
{
    /// <summary>Bir keçid hadisəsini emal edir: AccessEvent yazır, statusu yeniləyir.</summary>
    Task ProcessAsync(HikEventDto ev, CancellationToken ct = default);
}
