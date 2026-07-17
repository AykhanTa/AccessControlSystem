using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;
using AccessControlSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _db;
    public CardRepository(AppDbContext db) => _db = db;

    public Task<List<Card>> GetAllAsync(CancellationToken ct = default) =>
        _db.Cards
            .Include(c => c.Visits.Where(v => v.ActualExitAt == null))
                .ThenInclude(v => v.Guest)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    public Task<Card?> GetByIdAsync(long id, CancellationToken ct = default) =>
        _db.Cards.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> ExistsByNoAsync(string no, long? excludeId = null, CancellationToken ct = default) =>
        _db.Cards.AnyAsync(c => c.CardNo == no && (excludeId == null || c.Id != excludeId), ct);

    public Task<int> CountFreeActiveAsync(CancellationToken ct = default) =>
        _db.Cards.CountAsync(c => c.Status == CardStatus.Free && c.IsActive, ct);

    public Task<List<Card>> GetFreeActiveAsync(CancellationToken ct = default) =>
        _db.Cards.Where(c => c.Status == CardStatus.Free && c.IsActive)
            .OrderBy(c => c.CardNo).ToListAsync(ct);

    public async Task AddAsync(Card card, CancellationToken ct = default) =>
        await _db.Cards.AddAsync(card, ct);

    public void Remove(Card card) => _db.Cards.Remove(card);
}
