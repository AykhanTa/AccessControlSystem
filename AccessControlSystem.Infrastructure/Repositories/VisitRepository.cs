using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Repositories;

public class VisitRepository : IVisitRepository
{
    private readonly AppDbContext _db;
    public VisitRepository(AppDbContext db) => _db = db;

    private IQueryable<Visit> WithDetails() =>
        _db.Visits
            .Include(v => v.Guest)
            .Include(v => v.Host)
            .Include(v => v.Card)
            .Include(v => v.VisitAreas).ThenInclude(va => va.Area)
            .Include(v => v.VisitPurposes).ThenInclude(vp => vp.Purpose);

    public Task<List<Visit>> GetRegistryAsync(CancellationToken ct = default) =>
        WithDetails().OrderByDescending(v => v.ArrivalAt).ToListAsync(ct);

    public Task<List<Visit>> GetRecentAsync(int count, CancellationToken ct = default) =>
        WithDetails().OrderByDescending(v => v.CreatedAt).Take(count).ToListAsync(ct);

    public Task<List<Visit>> GetActivePermitsAsync(CancellationToken ct = default) =>
        WithDetails()
            .Where(v => v.Status == VisitStatus.In)   // yalnız binadadır; gecikənlər göstərilmir
            .OrderByDescending(v => v.ArrivalAt)
            .ToListAsync(ct);

    public Task<List<Visit>> GetHistoryAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var query = WithDetails().Where(v => v.ActualExitAt != null);
        if (from.HasValue) query = query.Where(v => v.ArrivalAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(v => v.ArrivalAt < to.Value.Date.AddDays(1));
        return query.OrderByDescending(v => v.ActualExitAt).ToListAsync(ct);
    }

    public Task<List<Visit>> GetForReportAsync(int year, CancellationToken ct = default) =>
        WithDetails().Where(v => v.ArrivalAt.Year == year).ToListAsync(ct);

    public Task<List<int>> GetDistinctYearsAsync(CancellationToken ct = default) =>
        _db.Visits.Select(v => v.ArrivalAt.Year).Distinct().OrderByDescending(y => y).ToListAsync(ct);

    public Task<Visit?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Visits.Include(v => v.Card).Include(v => v.Guest).FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task AddAsync(Visit visit, CancellationToken ct = default) =>
        await _db.Visits.AddAsync(visit, ct);

    public Task<int> CountTodayRegisteredAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return _db.Visits.CountAsync(v => v.CreatedAt >= today && v.CreatedAt < today.AddDays(1), ct);
    }

    public Task<int> CountCurrentlyInAsync(CancellationToken ct = default) =>
        _db.Visits.CountAsync(v => v.Status == VisitStatus.In, ct);

    public Task<int> CountTodayExitedAsync(CancellationToken ct = default)
    {
        var today = DateTime.Today;
        return _db.Visits.CountAsync(
            v => v.Status == VisitStatus.Out && v.ActualExitAt >= today && v.ActualExitAt < today.AddDays(1), ct);
    }

    public Task<int> CountLateAsync(CancellationToken ct = default) =>
        _db.Visits.CountAsync(v => v.Status == VisitStatus.Late, ct);
}
