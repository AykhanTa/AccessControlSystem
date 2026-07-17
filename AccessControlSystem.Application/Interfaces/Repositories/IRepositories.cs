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
    Task AddAsync(Visit visit, CancellationToken ct = default);

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
