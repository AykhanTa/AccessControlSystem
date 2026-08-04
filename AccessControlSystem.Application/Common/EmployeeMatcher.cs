using AccessControlSystem.Domain.Entities;

namespace AccessControlSystem.Application.Common;

/// <summary>
/// Cihaz hadisəsini (deviceId, employeeNoString, ad) işçiyə uyğunlaşdırır. Cihaz ID-ləri yalnız
/// CİHAZ DAXİLİNDƏ unikaldır — eyni nömrə fərqli cihazlarda fərqli şəxs ola bilər (məs. çıxış
/// cihazında 66 tamam başqa adam). Uyğunlaşma prioriteti:
/// 1) Açıq per-device alias (deviceId:nömrə);
/// 2) Cihazdakı AD (Employee.DeviceName) — cihaz hər hadisə ilə şəxsin adını göndərir; admin bu adı
///    dəqiq daxil edir → dəqiq uyğunlaşma (ad ↔ cihaz-adı, DB adı YOX → transliterasiya problemi olmur);
/// 3) Qlobal EmployeeNo/AccessNumber — AMMA hadisənin adı işçinin DeviceName-indən fərqlidirsə RƏDD
///    (nömrə üst-üstə düşsə də başqa adamdır). Beləliklə "66 çıxışda Ali Mammadov" Əlicavada yazılmır.
/// </summary>
public sealed class EmployeeMatcher
{
    private readonly Dictionary<(long, string), long> _byDevice = new();
    private readonly Dictionary<string, long> _byGlobal = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _ambiguousNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<long, string> _empName = new();   // işçi → cihazdakı adı (rədd yoxlaması)

    public EmployeeMatcher(IEnumerable<Employee> employees)
    {
        foreach (var e in employees)
        {
            foreach (var (did, num) in EmployeeMatch.ParseDeviceAliases(e.DeviceNumbers))
                _byDevice[(did, num)] = e.Id;
            foreach (var key in EmployeeMatch.GlobalKeys(e))
                _byGlobal.TryAdd(key, e.Id);

            if (string.IsNullOrWhiteSpace(e.DeviceName)) continue;
            var nm = e.DeviceName.Trim();
            _empName[e.Id] = nm;
            if (_byName.TryGetValue(nm, out var existing))
            {
                if (existing != e.Id) { _byName.Remove(nm); _ambiguousNames.Add(nm); }  // eyni ad iki işçidə
            }
            else if (!_ambiguousNames.Contains(nm)) _byName[nm] = e.Id;
        }
    }

    /// <summary>Hadisəni işçiyə uyğunlaşdırır (yoxdursa/əmin deyilsə null).</summary>
    public long? Resolve(long deviceId, string number, string? deviceName)
    {
        number = number.Trim();
        if (_byDevice.TryGetValue((deviceId, number), out var d)) return d;      // açıq per-device alias

        var name = deviceName?.Trim();
        if (!string.IsNullOrEmpty(name) && !_ambiguousNames.Contains(name) && _byName.TryGetValue(name, out var n))
            return n;                                                            // cihazdakı ad (dəqiq)

        if (_byGlobal.TryGetValue(number, out var g))
        {
            // Nömrə qlobal uyğunlaşsa da, hadisənin adı işçinin cihaz-adından fərqlidirsə → başqa adamdır.
            if (!string.IsNullOrEmpty(name) && _empName.TryGetValue(g, out var known)
                && !string.Equals(known, name, StringComparison.OrdinalIgnoreCase))
                return null;
            return g;
        }
        return null;
    }
}
