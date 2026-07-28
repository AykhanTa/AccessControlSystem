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
    private readonly IFloorRepository _floors;
    private readonly IPurposeRepository _purposes;
    private readonly IVisitAccessService _access;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;

    public GuestService(
        IVisitRepository visits, IGuestRepository guests, IHostRepository hosts,
        ICardRepository cards, IAreaRepository areas, IFloorRepository floors,
        IPurposeRepository purposes, IVisitAccessService access,
        IUnitOfWork uow, ISystemLogWriter log)
    {
        _visits = visits;
        _guests = guests;
        _hosts = hosts;
        _cards = cards;
        _areas = areas;
        _floors = floors;
        _purposes = purposes;
        _access = access;
        _uow = uow;
        _log = log;
    }

    public async Task<List<VisitRowDto>> GetRegistryAsync(CancellationToken ct = default)
    {
        var visits = await _visits.GetRegistryAsync(ct);
        return visits.Select(v => v.ToRow()).ToList();
    }

    public async Task<List<VisitStatusDto>> GetStatusesAsync(CancellationToken ct = default)
    {
        var pairs = await _visits.GetIdStatusesAsync(ct);
        return pairs.Select(p => new VisitStatusDto { Id = p.Id, Status = p.Status.ToKey() }).ToList();
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
        if (dto.FloorIds is null || dto.FloorIds.Count == 0)
            throw new ArgumentException("Ən azı bir mərtəbə seçilməlidir.");

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
            // Mövcud qonaq (eyni sənəd nömrəsi) — məlumatları ən son daxil edilənlə yenilə.
            guest.FirstName = dto.FirstName.Trim();
            guest.LastName = dto.LastName.Trim();
            guest.Patronymic = dto.Patronymic?.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Phone)) guest.Phone = dto.Phone.Trim();
            if (!string.IsNullOrWhiteSpace(dto.Email)) guest.Email = dto.Email.Trim();
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
            Status = VisitStatus.Planned,      // əvvəlcədən qeydiyyat — hələ binada deyil
            Note = dto.Note?.Trim()
        };

        // QR növündə nömrə + token dərhal generasiya olunur (QR əvvəlcədən çap/göndərilə bilər).
        // Kart növündə kart və nömrə CHECK-IN anında (nəzarətçi tərəfindən) təyin olunur.
        if (passType == PassType.Qr)
        {
            visit.QrToken = Guid.NewGuid().ToString("N");
            visit.AccessNumber = await GenerateAccessNumberAsync(ct);
        }

        // Mərtəbələr, (köhnə) ərazilər və məqsədlər (çox-çoxa)
        foreach (var floor in await _floors.GetByIdsAsync(dto.FloorIds, ct))
            visit.VisitFloors.Add(new VisitFloor { Floor = floor });
        foreach (var area in await _areas.GetByIdsAsync(dto.AreaIds, ct))
            visit.VisitAreas.Add(new VisitArea { Area = area });
        foreach (var purpose in await _purposes.GetByIdsAsync(dto.PurposeIds, ct))
            visit.VisitPurposes.Add(new VisitPurpose { Purpose = purpose });

        await _visits.AddAsync(visit, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("GUEST_REGISTERED", $"{guest.FullName} qonağı planlaşdırıldı.", "visit", visit.Id, ct: ct);
        return visit.Id;
    }

    public async Task CheckInAsync(long visitId, long? cardId, CancellationToken ct = default)
    {
        var visit = await _visits.GetForCheckInAsync(visitId, ct)
                    ?? throw new KeyNotFoundException("Ziyarət tapılmadı.");
        if (visit.Status is not (VisitStatus.Planned or VisitStatus.Late))
            throw new InvalidOperationException("Yalnız planlaşdırılmış qonaq üçün check-in edilə bilər.");

        if (visit.PassType == PassType.Card)
        {
            if (cardId is null)
                throw new ArgumentException("Kart seçilməlidir.");
            var card = await _cards.GetByIdAsync(cardId.Value, ct)
                       ?? throw new ArgumentException("Seçilmiş kart tapılmadı.");
            if (!card.IsActive)
                throw new ArgumentException("Deaktiv kart təyin edilə bilməz.");
            if (card.Status == CardStatus.Assigned)
                throw new ArgumentException("Bu kart artıq təyin edilib.");
            card.Status = CardStatus.Assigned;
            card.UpdatedAt = DateTime.Now;
            visit.CardId = card.Id;
            visit.AccessNumber = card.CardNo;   // vahid nömrə = kartın nömrəsi
        }
        else if (string.IsNullOrEmpty(visit.AccessNumber))
        {
            // QR növü — normalda qeydiyyatda təyin olunur; ehtiyat.
            visit.AccessNumber = await GenerateAccessNumberAsync(ct);
            visit.QrToken ??= Guid.NewGuid().ToString("N");
        }

        visit.Status = VisitStatus.CheckedIn;
        visit.CheckedInAt = DateTime.Now;
        visit.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("VISIT_CHECKIN",
            $"{visit.Guest?.FullName} üçün kart verildi və cihazlara yazıldı.", "visit", visit.Id, ct: ct);

        // İcazəli mərtəbələrin cihazlarına yaz (best-effort). Valid = indi → gözlənilən çıxış.
        var floorIds = visit.VisitFloors.Select(vf => vf.FloorId).ToList();
        var end = visit.ExpectedExitAt ?? DateTime.Now.AddHours(24);
        await _access.EnrollAsync(visit.Id, visit.AccessNumber!, visit.Guest?.FullName ?? "Qonaq",
            DateTime.Now, end, floorIds, ct);
    }

    /// <summary>Unikal, təxmin edilə bilməyən 10 rəqəmli AccessNumber (QR üçün).</summary>
    private async Task<string> GenerateAccessNumberAsync(CancellationToken ct)
    {
        while (true)
        {
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
            var num = BitConverter.ToUInt64(bytes) % 9_000_000_000UL + 1_000_000_000UL; // 10 rəqəm
            var s = num.ToString();
            if (!await _visits.AccessNumberExistsAsync(s, ct)) return s;
        }
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

        // Cihazlardan sil (best-effort).
        await _access.RevokeAsync(visit.Id, ct);
    }
}
