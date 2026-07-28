using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Mapping;

/// <summary>Domain entity → DTO çevirmələri.</summary>
public static class DomainMappings
{
    public static string ToKey(this VisitStatus s) => s switch
    {
        VisitStatus.In => "in",
        VisitStatus.Out => "out",
        VisitStatus.Late => "late",
        VisitStatus.Planned => "planned",
        VisitStatus.CheckedIn => "checkedin",
        VisitStatus.OnFloor => "onfloor",
        _ => "in"
    };

    public static string ToKey(this PassType p) => p == PassType.Qr ? "qr" : "card";

    public static VisitRowDto ToRow(this Visit v)
    {
        return new VisitRowDto
        {
            Id = v.Id,
            Name = v.Guest.FullName,
            Doc = v.Guest.IdDocument,
            Photo = v.Guest.PhotoPath,
            Host = v.Host.FullName,
            Arrival = AzDate.Format(v.ArrivalAt) ?? string.Empty,
            Exit = AzDate.Format(v.ActualExitAt ?? v.ExpectedExitAt),
            Area = string.Join(", ", v.VisitAreas.Select(a => a.Area.Name)),
            Floor = v.VisitFloors.Count > 0
                ? string.Join(", ", v.VisitFloors.Select(f => f.Floor.Name))
                : string.Join(", ", v.VisitAreas.Select(a => a.Area.Name)),   // köhnə ziyarətlər üçün ərazi fallback
            Purpose = string.Join(", ", v.VisitPurposes.Select(p => p.Purpose.Name)),
            Status = v.Status.ToKey(),
            PassType = v.PassType.ToKey(),
            CardNo = v.Card?.CardNo,
            AccessNumber = v.AccessNumber
        };
    }

    public static CardDto ToDto(this Card c)
    {
        // Aktiv (çıxış edilməmiş) ziyarətdəki qonaq — kartın kimə təyin olunduğunu göstərir.
        var activeVisit = c.Visits
            .Where(v => v.ActualExitAt == null)
            .OrderByDescending(v => v.ArrivalAt)
            .FirstOrDefault();

        return new CardDto
        {
            Id = c.Id,
            No = c.CardNo,
            Note = c.Note,
            Status = c.Status == CardStatus.Assigned ? "assigned" : "free",
            AssignedTo = activeVisit?.Guest?.FullName,
            Active = c.IsActive
        };
    }
}
