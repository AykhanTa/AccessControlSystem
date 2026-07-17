using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Application.Mapping;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class CardService : ICardService
{
    private readonly ICardRepository _cards;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public CardService(ICardRepository cards, IUnitOfWork uow, ISystemLogWriter log)
    {
        _cards = cards;
        _uow = uow;
        _log = log;
    }

    public async Task<List<CardDto>> GetAllAsync(CancellationToken ct = default)
    {
        var cards = await _cards.GetAllAsync(ct);
        return cards.Select(c => c.ToDto()).ToList();
    }

    public async Task<long> CreateAsync(CardCreateDto dto, CancellationToken ct = default)
    {
        var no = (dto.No ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(no))
            throw new ArgumentException("Kart nömrəsi daxil edin.");
        if (await _cards.ExistsByNoAsync(no, null, ct))
            throw new ArgumentException("Bu nömrəli kart artıq mövcuddur.");

        var card = new Card
        {
            CardNo = no,
            Note = dto.Note?.Trim(),
            Status = CardStatus.Free,
            IsActive = true
        };
        await _cards.AddAsync(card, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CARD_CREATED", $"{card.CardNo} kartı əlavə edildi.", "card", card.Id, ct: ct);
        return card.Id;
    }

    public async Task UpdateAsync(long id, CardUpdateDto dto, CancellationToken ct = default)
    {
        var card = await _cards.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Kart tapılmadı.");
        var no = (dto.No ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(no))
            throw new ArgumentException("Kart nömrəsi boş ola bilməz.");
        if (await _cards.ExistsByNoAsync(no, id, ct))
            throw new ArgumentException("Bu nömrəli kart artıq mövcuddur.");

        card.CardNo = no;
        card.Note = dto.Note?.Trim();
        card.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CARD_UPDATED", $"{card.CardNo} kartı yeniləndi.", "card", card.Id, ct: ct);
    }

    public async Task ToggleActiveAsync(long id, CancellationToken ct = default)
    {
        var card = await _cards.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Kart tapılmadı.");
        if (card.Status == CardStatus.Assigned)
            throw new InvalidOperationException("Təyin edilmiş kartın vəziyyəti dəyişdirilə bilməz.");

        card.IsActive = !card.IsActive;
        card.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CARD_STATUS_CHANGED",
            $"{card.CardNo} kartı {(card.IsActive ? "aktiv" : "deaktiv")} edildi.", "card", card.Id, ct: ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var card = await _cards.GetByIdAsync(id, ct)
                   ?? throw new KeyNotFoundException("Kart tapılmadı.");
        if (card.Status == CardStatus.Assigned)
            throw new InvalidOperationException("Təyin edilmiş kart silinə bilməz.");

        var no = card.CardNo;
        _cards.Remove(card);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("CARD_DELETED", $"{no} kartı silindi.", "card", id, ct: ct);
    }
}
