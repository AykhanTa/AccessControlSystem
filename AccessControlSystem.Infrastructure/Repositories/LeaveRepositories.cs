using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Repositories;

public class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly AppDbContext _db;
    public LeaveTypeRepository(AppDbContext db) => _db = db;

    public Task<List<LeaveType>> GetAllWithCompanyAsync(CancellationToken ct = default) =>
        _db.LeaveTypes.Include(t => t.Company).OrderBy(t => t.Name).ToListAsync(ct);

    public Task<List<LeaveType>> GetActiveAsync(CancellationToken ct = default) =>
        _db.LeaveTypes.Where(t => t.IsActive).OrderBy(t => t.Name).ToListAsync(ct);

    public Task<LeaveType?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.LeaveTypes.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<int> UsageCountAsync(long id, CancellationToken ct = default) =>
        _db.LeaveRecords.IgnoreQueryFilters().CountAsync(r => r.LeaveTypeId == id, ct);

    public async Task AddAsync(LeaveType type, CancellationToken ct = default) =>
        await _db.LeaveTypes.AddAsync(type, ct);

    public void Remove(LeaveType type) => _db.LeaveTypes.Remove(type);
}

public class LeaveRecordRepository : ILeaveRecordRepository
{
    private readonly AppDbContext _db;
    public LeaveRecordRepository(AppDbContext db) => _db = db;

    public Task<List<LeaveRecord>> GetForRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        _db.LeaveRecords
            .Include(r => r.LeaveType)
            .Include(r => r.Employee).ThenInclude(e => e!.Department)
            .Where(r => r.StartDate <= to && r.EndDate >= from)   // aralıqla kəsişən
            .OrderByDescending(r => r.StartDate)
            .ToListAsync(ct);

    public Task<LeaveRecord?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.LeaveRecords.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(LeaveRecord record, CancellationToken ct = default) =>
        await _db.LeaveRecords.AddAsync(record, ct);

    public void Remove(LeaveRecord record) => _db.LeaveRecords.Remove(record);
}

public class HolidayRepository : IHolidayRepository
{
    private readonly AppDbContext _db;
    public HolidayRepository(AppDbContext db) => _db = db;

    public Task<List<DateTime>> GetDatesAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        _db.Holidays.Where(h => h.Date >= from.Date && h.Date <= to.Date)
            .Select(h => h.Date).ToListAsync(ct);

    public Task<List<Holiday>> GetForRangeAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        _db.Holidays.Include(h => h.Company)
            .Where(h => h.Date >= from.Date && h.Date <= to.Date)
            .OrderBy(h => h.Date).ToListAsync(ct);

    public Task<Holiday?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Holidays.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<bool> ExistsAsync(long? companyId, DateTime date, CancellationToken ct = default) =>
        _db.Holidays.AnyAsync(h => h.CompanyId == companyId && h.Date == date.Date, ct);

    public async Task AddAsync(Holiday holiday, CancellationToken ct = default) =>
        await _db.Holidays.AddAsync(holiday, ct);

    public void Remove(Holiday holiday) => _db.Holidays.Remove(holiday);
}
