using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class ReportService : IReportService
{
    private readonly IVisitRepository _visits;

    private static readonly string[] MonthNames =
        { "yanvar", "fevral", "mart", "aprel", "may", "iyun",
          "iyul", "avqust", "sentyabr", "oktyabr", "noyabr", "dekabr" };

    public ReportService(IVisitRepository visits) => _visits = visits;

    public Task<List<int>> GetYearsAsync(CancellationToken ct = default) =>
        _visits.GetDistinctYearsAsync(ct);

    public async Task<ReportDto> GetReportAsync(int year, CancellationToken ct = default)
    {
        var visits = await _visits.GetForReportAsync(year, ct);

        var exits = visits.Count(v => v.Status == VisitStatus.Out);
        var entries = visits.Count;   // hər ziyarətin gəlişi var

        // Orta qalma müddəti — yalnız müsbət müddətli, çıxışı təsdiqlənmiş ziyarətlər
        var durations = visits
            .Where(v => v.Status == VisitStatus.Out && v.ActualExitAt != null)
            .Select(v => (v.ActualExitAt!.Value - v.ArrivalAt).TotalMinutes)
            .Where(d => d > 0)
            .ToList();

        // Aylıq jurnal (12 ay, sıfırlar da göstərilir)
        var monthCounts = new int[12];
        foreach (var v in visits) monthCounts[v.ArrivalAt.Month - 1]++;
        var months = MonthNames
            .Select((name, i) => new LabelCount { Label = name, Count = monthCounts[i] })
            .ToList();

        return new ReportDto
        {
            Year = year,
            TotalVisits = visits.Count,
            UniqueGuests = visits.Select(v => v.GuestId).Distinct().Count(),
            Entries = entries,
            Exits = exits,
            CardUse = visits.Count(v => v.PassType == PassType.Card),
            QrUse = visits.Count(v => v.PassType == PassType.Qr),
            Inside = visits.Count(v => v.Status is VisitStatus.In or VisitStatus.Late),
            Late = visits.Count(v => v.Status == VisitStatus.Late),
            ExitRatePercent = entries > 0 ? (int)Math.Round(exits * 100.0 / entries) : 0,
            AvgStayMinutes = durations.Count > 0 ? (int)Math.Round(durations.Average()) : 0,
            Months = months,
            Purposes = Tally(visits.SelectMany(v => v.VisitPurposes.Select(p => p.Purpose.Name))),
            Hosts = Tally(visits.Select(v => v.Host.FullName)),
            Areas = Tally(visits.SelectMany(v => v.VisitAreas.Select(a => a.Area.Name))),
            Cards = Tally(visits.Where(v => v.PassType == PassType.Card && v.Card != null)
                                 .Select(v => v.Card!.CardNo))
        };
    }

    /// <summary>Etiketləri sayır, saya görə azalan, sonra ada görə sıralayır.</summary>
    private static List<LabelCount> Tally(IEnumerable<string> items) =>
        items.Where(s => !string.IsNullOrWhiteSpace(s))
             .GroupBy(s => s)
             .Select(g => new LabelCount { Label = g.Key, Count = g.Count() })
             .OrderByDescending(x => x.Count)
             .ThenBy(x => x.Label, StringComparer.CurrentCulture)
             .ToList();
}
