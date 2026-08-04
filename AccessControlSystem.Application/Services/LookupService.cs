using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;

namespace AccessControlSystem.Application.Services;

public class LookupService : ILookupService
{
    private readonly IHostRepository _hosts;
    private readonly IAreaRepository _areas;
    private readonly IPurposeRepository _purposes;
    private readonly ICardRepository _cards;
    private readonly IFloorRepository _floors;

    public LookupService(IHostRepository hosts, IAreaRepository areas, IPurposeRepository purposes,
        ICardRepository cards, IFloorRepository floors)
    {
        _hosts = hosts;
        _areas = areas;
        _purposes = purposes;
        _cards = cards;
        _floors = floors;
    }

    public async Task<List<LookupDto>> GetHostsAsync(CancellationToken ct = default) =>
        (await _hosts.GetActiveAsync(ct)).Select(h => new LookupDto { Id = h.Id, Name = h.FullName, CompanyId = h.CompanyId }).ToList();

    public async Task<List<LookupDto>> GetAreasAsync(CancellationToken ct = default) =>
        (await _areas.GetActiveAsync(ct)).Select(a => new LookupDto { Id = a.Id, Name = a.Name }).ToList();

    public async Task<List<LookupDto>> GetPurposesAsync(CancellationToken ct = default) =>
        (await _purposes.GetActiveAsync(ct)).Select(p => new LookupDto { Id = p.Id, Name = p.Name }).ToList();

    public async Task<List<LookupDto>> GetFreeCardsAsync(CancellationToken ct = default) =>
        (await _cards.GetFreeActiveAsync(ct)).Select(c => new LookupDto { Id = c.Id, Name = c.CardNo }).ToList();

    public async Task<List<LookupDto>> GetFloorsAsync(CancellationToken ct = default) =>
        (await _floors.GetActiveAsync(ct)).Select(f => new LookupDto { Id = f.Id, Name = f.Name }).ToList();
}
