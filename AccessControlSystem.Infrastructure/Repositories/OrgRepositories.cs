using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly AppDbContext _db;
    public EmployeeRepository(AppDbContext db) => _db = db;

    public Task<List<string>> GetAllAccessNumbersAsync(CancellationToken ct = default) =>
        _db.Employees.IgnoreQueryFilters()
            .Where(e => e.AccessNumber != null && e.AccessNumber != "")
            .Select(e => e.AccessNumber!)
            .ToListAsync(ct);

    public Task<List<Employee>> GetAllAsync(CancellationToken ct = default) =>
        _db.Employees
            .Include(e => e.Company)
            .Include(e => e.Department).ThenInclude(d => d!.WorkSchedule)
            .Include(e => e.Position)
            .Include(e => e.WorkSchedule)
            .Include(e => e.EmployeeFloors).ThenInclude(ef => ef.Floor)
            .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
            .ToListAsync(ct);

    public Task<Employee?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Employees
            .Include(e => e.EmployeeFloors)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<bool> ExistsByEmployeeNoAsync(string employeeNo, long? excludeId = null, CancellationToken ct = default) =>
        _db.Employees.AnyAsync(e => e.EmployeeNo == employeeNo && (excludeId == null || e.Id != excludeId), ct);

    public Task<Employee?> GetActiveByAccessNumberAsync(string accessNumber, CancellationToken ct = default)
    {
        accessNumber = accessNumber.Trim();
        // Yalnız QLOBAL unikal açarlar (AccessNumber "9"+Id, ya da Tabel nömrəsi/EmployeeNo).
        // Cihaza-bağlı alias-lar (cihazId:nömrə) burada uyğunlaşdırılmır — cihaz konteksti yoxdur;
        // onları DeviceEventPoller.ReconcilePresenceAsync cihaz-əsaslı emal edir (qarışma olmasın).
        return _db.Employees.FirstOrDefaultAsync(
            e => (e.AccessNumber == accessNumber || e.EmployeeNo == accessNumber) && e.Status == EmployeeStatus.Active, ct);
    }

    public async Task<List<(long Id, PresenceStatus Presence, DateTime? LastSeen)>> GetPresencePairsAsync(CancellationToken ct = default)
    {
        var rows = await _db.Employees.Select(e => new { e.Id, e.CurrentPresence, e.LastSeenAt }).ToListAsync(ct);
        return rows.Select(r => (r.Id, r.CurrentPresence, r.LastSeenAt)).ToList();
    }

    public Task<int> CountAsync(CancellationToken ct = default) => _db.Employees.CountAsync(ct);

    public async Task AddAsync(Employee employee, CancellationToken ct = default) =>
        await _db.Employees.AddAsync(employee, ct);

    public void Remove(Employee employee) => _db.Employees.Remove(employee);
}

public class CompanyRepository : ICompanyRepository
{
    private readonly AppDbContext _db;
    public CompanyRepository(AppDbContext db) => _db = db;

    public Task<List<Company>> GetAllAsync(CancellationToken ct = default) =>
        _db.Companies.OrderBy(c => c.Name).ToListAsync(ct);

    public Task<List<Company>> GetActiveAsync(CancellationToken ct = default) =>
        _db.Companies.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Company?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Companies.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<int> DepartmentCountAsync(long companyId, CancellationToken ct = default) =>
        _db.Departments.CountAsync(d => d.CompanyId == companyId, ct);

    public async Task<bool> HasDependentsAsync(long companyId, CancellationToken ct = default) =>
        await _db.Departments.AnyAsync(d => d.CompanyId == companyId, ct)
        || await _db.Positions.AnyAsync(p => p.CompanyId == companyId, ct);

    public async Task AddAsync(Company company, CancellationToken ct = default) =>
        await _db.Companies.AddAsync(company, ct);

    public void Remove(Company company) => _db.Companies.Remove(company);
}

public class DepartmentRepository : IDepartmentRepository
{
    private readonly AppDbContext _db;
    public DepartmentRepository(AppDbContext db) => _db = db;

    public Task<List<Department>> GetAllWithCompanyAsync(CancellationToken ct = default) =>
        _db.Departments.Include(d => d.Company).Include(d => d.ParentDepartment).Include(d => d.WorkSchedule)
            .OrderBy(d => d.Company.Name).ThenBy(d => d.Name).ToListAsync(ct);

    public Task<List<Department>> GetActiveByCompanyAsync(long companyId, CancellationToken ct = default) =>
        _db.Departments.Where(d => d.IsActive && d.CompanyId == companyId).OrderBy(d => d.Name).ToListAsync(ct);

    public Task<Department?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task AddAsync(Department department, CancellationToken ct = default) =>
        await _db.Departments.AddAsync(department, ct);

    public void Remove(Department department) => _db.Departments.Remove(department);
}

public class PositionRepository : IPositionRepository
{
    private readonly AppDbContext _db;
    public PositionRepository(AppDbContext db) => _db = db;

    public Task<List<Position>> GetAllWithCompanyAsync(CancellationToken ct = default) =>
        _db.Positions.Include(p => p.Company)
            .OrderBy(p => p.Company.Name).ThenBy(p => p.Name).ToListAsync(ct);

    public Task<Position?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Positions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Position position, CancellationToken ct = default) =>
        await _db.Positions.AddAsync(position, ct);

    public void Remove(Position position) => _db.Positions.Remove(position);
}

public class WorkScheduleRepository : IWorkScheduleRepository
{
    private readonly AppDbContext _db;
    public WorkScheduleRepository(AppDbContext db) => _db = db;

    public Task<List<WorkSchedule>> GetAllWithCompanyAsync(CancellationToken ct = default) =>
        _db.WorkSchedules.Include(s => s.Company).Where(s => s.OwnerEmployeeId == null)
            .OrderBy(s => s.Name).ToListAsync(ct);

    public Task<List<WorkSchedule>> GetActiveAsync(CancellationToken ct = default) =>
        _db.WorkSchedules.Where(s => s.IsActive && s.OwnerEmployeeId == null)
            .OrderBy(s => s.Name).ToListAsync(ct);

    public Task<WorkSchedule?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.WorkSchedules.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<WorkSchedule?> GetPersonalByEmployeeAsync(long employeeId, CancellationToken ct = default) =>
        _db.WorkSchedules.FirstOrDefaultAsync(s => s.OwnerEmployeeId == employeeId, ct);

    public async Task<int> UsageCountAsync(long id, CancellationToken ct = default) =>
        await _db.Employees.IgnoreQueryFilters().CountAsync(e => e.WorkScheduleId == id, ct)
        + await _db.Departments.IgnoreQueryFilters().CountAsync(d => d.WorkScheduleId == id, ct);

    public async Task AddAsync(WorkSchedule schedule, CancellationToken ct = default) =>
        await _db.WorkSchedules.AddAsync(schedule, ct);

    public void Remove(WorkSchedule schedule) => _db.WorkSchedules.Remove(schedule);
}
