using AccessControlSystem.Domain.Entities;

namespace AccessControlSystem.Application.Common;

/// <summary>
/// İşçini cihaz hadisələrindəki nömrə (employeeNoString) ilə uyğunlaşdırma köməkçisi.
/// Cihaz ID-ləri yalnız CİHAZ DAXİLİNDƏ unikaldır — eyni nömrə fərqli cihazlarda fərqli
/// şəxs ola bilər. Ona görə alias-lar CİHAZA BAĞLIDIR: "cihazId:nömrə" (məs. "3:62,2:64").
/// Uyğunlaşma: əvvəl cihaz-spesifik (deviceId, nömrə), sonra qlobal (EmployeeNo/AccessNumber).
/// </summary>
public static class EmployeeMatch
{
    private static readonly char[] Separators = { ',', ';', '\n', '\r', '\t', '|' };

    /// <summary>DeviceNumbers sətrini ayrı-ayrı hissələrə bölür ("3:62","2:64").</summary>
    private static IEnumerable<string> Split(string? deviceNumbers) =>
        string.IsNullOrWhiteSpace(deviceNumbers)
            ? Enumerable.Empty<string>()
            : deviceNumbers.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Cihaza bağlı alias-lar: (deviceId, nömrə). Yalnız "cihazId:nömrə" formatlı hissələr.</summary>
    public static IEnumerable<(long deviceId, string number)> ParseDeviceAliases(string? deviceNumbers)
    {
        foreach (var part in Split(deviceNumbers))
        {
            var i = part.IndexOf(':');
            if (i <= 0) continue;
            if (!long.TryParse(part[..i].Trim(), out var did)) continue;
            var num = part[(i + 1)..].Trim();
            if (num.Length > 0) yield return (did, num);
        }
    }

    /// <summary>Qlobal (cihaz-agnostik) unikal açarlar: EmployeeNo + AccessNumber.</summary>
    public static IEnumerable<string> GlobalKeys(Employee e)
    {
        if (!string.IsNullOrWhiteSpace(e.EmployeeNo)) yield return e.EmployeeNo.Trim();
        if (!string.IsNullOrWhiteSpace(e.AccessNumber)) yield return e.AccessNumber!.Trim();
    }
}
