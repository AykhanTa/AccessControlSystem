using AccessControlSystem.Domain.Entities;

namespace AccessControlSystem.Application.Interfaces.Repositories;

/// <summary>Dəyişiklikləri bazaya yazan vahid iş (Unit of Work).</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}

public interface IVisitRepository
{
    /// <summary>Bütün ziyarətlər (Guest, Host, Card, Ərazi, Məqsəd daxil), gəlişə görə azalan.</summary>
    Task<List<Visit>> GetRegistryAsync(CancellationToken ct = default);
    Task<List<Visit>> GetRecentAsync(int count, CancellationToken ct = default);
    /// <summary>Aktiv icazələr — hazırda binada olan (in/late) ziyarətlər.</summary>
    Task<List<Visit>> GetActivePermitsAsync(CancellationToken ct = default);
    /// <summary>Giriş-çıxış tarixçəsi — çıxışı tamamlanmış ziyarətlər (tarix aralığı ilə).</summary>
    Task<List<Visit>> GetHistoryAsync(DateTime? from, DateTime? to, CancellationToken ct = default);
    /// <summary>Verilmiş il üzrə bütün ziyarətlər (hesabat üçün, detallarla).</summary>
    Task<List<Visit>> GetForReportAsync(int year, CancellationToken ct = default);
    /// <summary>Ziyarətlərin mövcud olduğu illər (azalan).</summary>
    Task<List<int>> GetDistinctYearsAsync(CancellationToken ct = default);
    Task<Visit?> GetByIdAsync(long id, CancellationToken ct = default);
    /// <summary>Check-in üçün — Guest və VisitFloors daxil.</summary>
    Task<Visit?> GetForCheckInAsync(long id, CancellationToken ct = default);
    /// <summary>AccessNumber üzrə hazırda aktiv ziyarət (Guest+Card daxil). Event emalı üçün.</summary>
    Task<Visit?> GetActiveByAccessNumberAsync(string accessNumber, CancellationToken ct = default);
    /// <summary>Canlı status feed üçün — bütün ziyarətlərin id + status cütləri.</summary>
    Task<List<(long Id, Domain.Enums.VisitStatus Status)>> GetIdStatusesAsync(CancellationToken ct = default);
    /// <summary>Background təmizləmə üçün — aktiv və ya hələ cihazda qeydi qalan çıxmış ziyarətlər
    /// (Guest, Card, DeviceEnrollments.Device daxil).</summary>
    Task<List<Visit>> GetForMaintenanceAsync(CancellationToken ct = default);
    /// <summary>Verilmiş AccessNumber-i başqa AKTİV ziyarət istifadə edirmi (kart təkrar istifadə qorunması).</summary>
    Task<bool> HasOtherActiveWithAccessNumberAsync(string accessNumber, long excludeVisitId, CancellationToken ct = default);
    Task AddAsync(Visit visit, CancellationToken ct = default);
    /// <summary>Verilmiş AccessNumber artıq mövcuddurmu (unikallıq üçün).</summary>
    Task<bool> AccessNumberExistsAsync(string accessNumber, CancellationToken ct = default);

    Task<int> CountTodayRegisteredAsync(CancellationToken ct = default);
    Task<int> CountCurrentlyInAsync(CancellationToken ct = default);
    Task<int> CountTodayExitedAsync(CancellationToken ct = default);
    Task<int> CountLateAsync(CancellationToken ct = default);
}

public interface ICardRepository
{
    /// <summary>Bütün kartlar (aktiv ziyarətdəki qonaq daxil).</summary>
    Task<List<Card>> GetAllAsync(CancellationToken ct = default);
    Task<Card?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByNoAsync(string no, long? excludeId = null, CancellationToken ct = default);
    Task<int> CountFreeActiveAsync(CancellationToken ct = default);
    Task<List<Card>> GetFreeActiveAsync(CancellationToken ct = default);
    Task AddAsync(Card card, CancellationToken ct = default);
    void Remove(Card card);
}

public interface IGuestRepository
{
    Task<Guest?> GetByDocumentAsync(string idDocument, CancellationToken ct = default);
    Task AddAsync(Guest guest, CancellationToken ct = default);
}

public interface IHostRepository
{
    Task<List<Host>> GetActiveAsync(CancellationToken ct = default);
    Task<List<Host>> GetAllAsync(CancellationToken ct = default);
    Task<Host?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsAsync(long id, CancellationToken ct = default);
    Task AddAsync(Host host, CancellationToken ct = default);
    void Remove(Host host);
}

public interface IAreaRepository
{
    Task<List<Area>> GetActiveAsync(CancellationToken ct = default);
    Task<List<Area>> GetAllAsync(CancellationToken ct = default);
    Task<Area?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<Area>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task<int> UsageCountAsync(long areaId, CancellationToken ct = default);
    Task AddAsync(Area area, CancellationToken ct = default);
    void Remove(Area area);
}

public interface IPurposeRepository
{
    Task<List<Purpose>> GetActiveAsync(CancellationToken ct = default);
    Task<List<Purpose>> GetAllAsync(CancellationToken ct = default);
    Task<Purpose?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<Purpose>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default);
    Task AddAsync(Purpose purpose, CancellationToken ct = default);
}

public interface IFloorRepository
{
    Task<List<Floor>> GetActiveAsync(CancellationToken ct = default);
    Task<List<Floor>> GetAllAsync(CancellationToken ct = default);
    Task<List<Floor>> GetByIdsAsync(IEnumerable<long> ids, CancellationToken ct = default);
    Task<Floor?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default);
    Task<int> DeviceCountAsync(long floorId, CancellationToken ct = default);
    Task<int> VisitUsageCountAsync(long floorId, CancellationToken ct = default);
    Task AddAsync(Floor floor, CancellationToken ct = default);
    void Remove(Floor floor);
}

public interface IAccessEventRepository
{
    Task AddAsync(AccessEvent ev, CancellationToken ct = default);
    /// <summary>Son N hadisə (Device daxil) — diaqnostika üçün.</summary>
    Task<List<AccessEvent>> GetRecentAsync(int take, CancellationToken ct = default);
}

public interface IDeviceRepository
{
    /// <summary>Verilmiş mərtəbələrin bütün aktiv cihazları (giriş + çıxış).</summary>
    Task<List<Device>> GetActiveByFloorIdsAsync(IEnumerable<long> floorIds, CancellationToken ct = default);
    /// <summary>IP üzrə cihaz (Floor daxil). Event mənbəyini tapmaq üçün.</summary>
    Task<Device?> GetByIpAsync(string ip, CancellationToken ct = default);
    /// <summary>Bütün aktiv cihazlar — poll üçün.</summary>
    Task<List<Device>> GetAllActiveAsync(CancellationToken ct = default);
    Task<List<Device>> GetAllWithFloorAsync(CancellationToken ct = default);
    Task<Device?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<bool> ExistsByIpPortAsync(string ip, int port, long? excludeId = null, CancellationToken ct = default);
    Task AddAsync(Device device, CancellationToken ct = default);
    void Remove(Device device);
}

public interface IDeviceEnrollmentRepository
{
    /// <summary>Ziyarətin cihaz-qeydləri (Device daxil).</summary>
    Task<List<DeviceEnrollment>> GetByVisitAsync(long visitId, CancellationToken ct = default);
    Task AddAsync(DeviceEnrollment enrollment, CancellationToken ct = default);
}

public interface ISectionRepository
{
    Task<List<Section>> GetAllOrderedAsync(CancellationToken ct = default);
}

public interface IRoleRepository
{
    Task<List<Role>> GetAllWithPermissionsAsync(CancellationToken ct = default);
    Task<Role?> GetByIdWithPermissionsAsync(long id, CancellationToken ct = default);
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<bool> ExistsByNameAsync(string name, long? excludeId = null, CancellationToken ct = default);
    Task<int> CountUsersAsync(long roleId, CancellationToken ct = default);
    Task AddAsync(Role role, CancellationToken ct = default);
    void Remove(Role role);
}

public interface ISystemLogRepository
{
    Task AddAsync(SystemLog log, CancellationToken ct = default);
    /// <summary>Axtarışa uyğun loqlar (yenidən köhnəyə), skip/take ilə səhifələnmiş.</summary>
    Task<List<SystemLog>> GetPagedAsync(string? search, int skip, int take, CancellationToken ct = default);
    /// <summary>Axtarışa uyğun ümumi loq sayı.</summary>
    Task<int> CountAsync(string? search, CancellationToken ct = default);
}

public interface IUserRepository
{
    Task<List<AppUser>> GetAllWithRoleAsync(CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExistsByEmailAsync(string email, long? excludeId = null, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
    void Remove(AppUser user);
}
