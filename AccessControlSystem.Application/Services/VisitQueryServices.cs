using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Application.Mapping;

namespace AccessControlSystem.Application.Services;

public class ActivePermitService : IActivePermitService
{
    private readonly IVisitRepository _visits;
    public ActivePermitService(IVisitRepository visits) => _visits = visits;

    public async Task<List<VisitRowDto>> GetActiveAsync(CancellationToken ct = default)
    {
        var visits = await _visits.GetActivePermitsAsync(ct);
        return visits.Select(v => v.ToRow()).ToList();
    }
}

public class HistoryService : IHistoryService
{
    private readonly IVisitRepository _visits;
    public HistoryService(IVisitRepository visits) => _visits = visits;

    public async Task<List<VisitRowDto>> GetHistoryAsync(DateTime? from, DateTime? to, CancellationToken ct = default)
    {
        var visits = await _visits.GetHistoryAsync(from, to, ct);
        return visits.Select(v => v.ToRow()).ToList();
    }
}
