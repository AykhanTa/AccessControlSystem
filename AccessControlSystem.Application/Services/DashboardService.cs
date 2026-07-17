using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Application.Mapping;

namespace AccessControlSystem.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IVisitRepository _visits;
    private readonly ICardRepository _cards;

    public DashboardService(IVisitRepository visits, ICardRepository cards)
    {
        _visits = visits;
        _cards = cards;
    }

    public async Task<DashboardStatsDto> GetStatsAsync(CancellationToken ct = default)
    {
        return new DashboardStatsDto
        {
            TodayRegistered = await _visits.CountTodayRegisteredAsync(ct),
            CurrentlyIn = await _visits.CountCurrentlyInAsync(ct),
            TodayExited = await _visits.CountTodayExitedAsync(ct),
            LateExits = await _visits.CountLateAsync(ct),
            FreeCards = await _cards.CountFreeActiveAsync(ct),
            ActiveSessions = await _visits.CountCurrentlyInAsync(ct) // sadələşdirilmiş: binadakı aktiv ziyarətlər
        };
    }

    public async Task<List<VisitRowDto>> GetRecentGuestsAsync(int count = 10, CancellationToken ct = default)
    {
        var visits = await _visits.GetRecentAsync(count, ct);
        return visits.Select(v => v.ToRow()).ToList();
    }
}
