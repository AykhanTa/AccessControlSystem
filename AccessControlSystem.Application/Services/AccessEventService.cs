using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

/// <summary>Keçid hadisələri (giriş-çıxış log) — qonaq + işçi birlikdə, ən yenidən köhnəyə.</summary>
public class AccessEventService : IAccessEventService
{
    private readonly IAccessEventRepository _events;
    public AccessEventService(IAccessEventRepository events) => _events = events;

    public async Task<List<AccessEventRowDto>> GetByDayAsync(DateTime? day = null, CancellationToken ct = default)
    {
        var rows = await _events.GetByDayDetailedAsync((day ?? DateTime.Today).Date, ct);
        return rows.Select(e => new AccessEventRowDto
        {
            Id = e.Id,
            Time = e.OccurredAt.ToString("dd.MM.yyyy HH:mm:ss"),
            AccessNumber = e.AccessNumber,
            Name = e.PersonName
                   ?? (e.Visit?.Guest is { } g ? $"{g.FirstName} {g.LastName}".Trim() : null)
                   ?? e.Employee?.FullName,
            PersonType = e.VisitId != null ? "guest" : e.EmployeeId != null ? "employee" : "",
            Granted = e.Granted,
            Point = e.Device?.AccessPoint?.Name ?? e.Device?.Name,
            Direction = e.Device == null
                ? null
                : (e.Device.AccessPoint?.Direction ?? e.Device.Direction) == DeviceDirection.Exit ? "exit" : "entry"
        }).ToList();
    }
}
