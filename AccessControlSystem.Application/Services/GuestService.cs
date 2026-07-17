using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Application.Mapping;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class GuestService : IGuestService
{
    private readonly IVisitRepository _visits;
    private readonly IGuestRepository _guests;
    private readonly IHostRepository _hosts;
    private readonly ICardRepository _cards;
    private readonly IAreaRepository _areas;
    private readonly IPurposeRepository _purposes;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public GuestService(
        IVisitRepository visits, IGuestRepository guests, IHostRepository hosts,
        ICardRepository cards, IAreaRepository areas, IPurposeRepository purposes,
        IUnitOfWork uow, ISystemLogWriter log)
    {
        _visits = visits;
        _guests = guests;
        _hosts = hosts;
        _cards = cards;
        _areas = areas;
        _purposes = purposes;
        _uow = uow;
        _log = log;
    }

    public async Task<List<VisitRowDto>> GetRegistryAsync(CancellationToken ct = default)
    {
        var visits = await _visits.GetRegistryAsync(ct);
        return visits.Select(v => v.ToRow()).ToList();
    }

    public async Task<long> RegisterAsync(GuestCreateDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            throw new ArgumentException("Ad və Soyad mütləq doldurulmalıdır.");
        if (string.IsNullOrWhiteSpace(dto.IdDocument))
            throw new ArgumentException("Şəxsiyyət vəsiqəsi mütləq doldurulmalıdır.");
        if (dto.ArrivalAt.Date < DateTime.Today)
            throw new ArgumentException("Keçmiş tarixə yeni qonaq yaradıla bilməz.");
        if (dto.ExpectedExitAt is not null && dto.ExpectedExitAt < dto.ArrivalAt)
            throw new ArgumentException("Çıxış tarixi gəliş tarixindən əvvəl ola bilməz.");
        if (!await _hosts.ExistsAsync(dto.HostId, ct))
            throw new ArgumentException("Qəbul edən şəxs tapılmadı.");

        var passType = dto.PassType?.ToLowerInvariant() == "qr" ? PassType.Qr : PassType.Card;

        // Qonaq — sənəd nömrəsi üzrə mövcuddursa təkrar istifadə et, yoxdursa yarat.
        var guest = await _guests.GetByDocumentAsync(dto.IdDocument.Trim(), ct);
        if (guest is null)
        {
            guest = new Guest
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Patronymic = dto.Patronymic?.Trim(),
                IdDocument = dto.IdDocument.Trim(),
                Phone = dto.Phone?.Trim(),
                Email = dto.Email?.Trim(),
                PhotoPath = dto.PhotoPath,
                DocumentPath = dto.DocumentPath
            };
            await _guests.AddAsync(guest, ct);
        }
        else
        {
            // Mövcud qonaq — yeni yüklənmiş fayllar varsa yenilə
            if (!string.IsNullOrWhiteSpace(dto.PhotoPath)) guest.PhotoPath = dto.PhotoPath;
            if (!string.IsNullOrWhiteSpace(dto.DocumentPath)) guest.DocumentPath = dto.DocumentPath;
            guest.UpdatedAt = DateTime.Now;
        }

        var visit = new Visit
        {
            Guest = guest,
            HostId = dto.HostId,
            PassType = passType,
            ArrivalAt = dto.ArrivalAt,
            ExpectedExitAt = dto.ExpectedExitAt,
            Status = VisitStatus.In,
            Note = dto.Note?.Trim()
        };

        if (passType == PassType.Card)
        {
            if (dto.CardId is null)
                throw new ArgumentException("Kartla buraxılış üçün kart seçilməlidir.");
            var card = await _cards.GetByIdAsync(dto.CardId.Value, ct)
                       ?? throw new ArgumentException("Seçilmiş kart tapılmadı.");
            if (!card.IsActive)
                throw new ArgumentException("Deaktiv kart təyin edilə bilməz.");
            if (card.Status == CardStatus.Assigned)
                throw new ArgumentException("Bu kart artıq təyin edilib.");
            card.Status = CardStatus.Assigned;
            card.UpdatedAt = DateTime.Now;
            visit.CardId = card.Id;
        }
        else
        {
            visit.QrToken = Guid.NewGuid().ToString("N");
        }

        // Ərazi və məqsədlər (çox-çoxa)
        foreach (var area in await _areas.GetByIdsAsync(dto.AreaIds, ct))
            visit.VisitAreas.Add(new VisitArea { Area = area });
        foreach (var purpose in await _purposes.GetByIdsAsync(dto.PurposeIds, ct))
            visit.VisitPurposes.Add(new VisitPurpose { Purpose = purpose });

        await _visits.AddAsync(visit, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("GUEST_REGISTERED", $"{guest.FullName} qonağı qeydiyyata alındı.", "visit", visit.Id, ct: ct);
        return visit.Id;
    }

    public async Task CheckOutAsync(long visitId, CancellationToken ct = default)
    {
        var visit = await _visits.GetByIdAsync(visitId, ct)
                    ?? throw new KeyNotFoundException("Ziyarət tapılmadı.");
        if (visit.Status == VisitStatus.Out)
            return;

        visit.Status = VisitStatus.Out;
        visit.ActualExitAt = DateTime.Now;
        visit.UpdatedAt = DateTime.Now;

        // Kart varsa boşalt
        if (visit.CardId is not null && visit.Card is not null)
        {
            visit.Card.Status = CardStatus.Free;
            visit.Card.UpdatedAt = DateTime.Now;
        }

        await _uow.SaveChangesAsync(ct);
        var guestName = visit.Guest?.FullName ?? $"Ziyarət #{visit.Id}";
        await _log.LogAsync("VISIT_CHECKOUT", $"{guestName} üçün çıxış təsdiqləndi.", "visit", visit.Id, ct: ct);
    }
}
