namespace AccessControlSystem.Application.Common;

/// <summary>UI üçün Azərbaycan formatında tarix/vaxt mətni: "9 iyl 2026, 16:00".</summary>
public static class AzDate
{
    private static readonly string[] Months =
        { "yan", "fev", "mar", "apr", "may", "iyn", "iyl", "avq", "sen", "okt", "noy", "dek" };

    public static string? Format(DateTime? dt)
    {
        if (dt is null) return null;
        var d = dt.Value;
        return $"{d.Day} {Months[d.Month - 1]} {d.Year}, {d:HH:mm}";
    }
}
