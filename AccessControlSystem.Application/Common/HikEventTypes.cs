namespace AccessControlSystem.Application.Common;

/// <summary>
/// Hikvision AcsEvent (major/minor) kodlarını oxunaqlı etiketə çevirir.
/// Kodlar firmware-ə görə dəyişə bilər — bilmədiyimiz kodlar üçün "Kod major/minor" fallback.
/// Cihazın real kodlarını /AccessEvents/Codes diaqnostikası ilə tutub buranı dəqiqləşdirmək olar.
/// </summary>
public static class HikEventTypes
{
    // (major, minor) → etiket. Əminliklə bildiyimiz kodlar.
    private static readonly Dictionary<(int, int), string> Map = new()
    {
        // Üz / kart autentifikasiyası (bu cihazda müşahidə olunub)
        [(5, 75)] = "Üz ilə təsdiqləndi",
        [(5, 76)] = "Üz təsdiqi uğursuz",
        [(5, 1)]  = "Kart ilə təsdiqləndi",

        // Qapı hadisələri
        [(5, 21)] = "Qapı açıldı",
        [(5, 22)] = "Qapı bağlandı",
        [(5, 23)] = "Qapı açıq qaldı (xəbərdarlıq)",

        // Cihazın real kodları (2026-07-30, Hikvision jurnalı ilə tutuşdurulub)
        [(3, 112)]  = "Uzaqdan giriş (Login)",   // Remote: Login
        [(1, 1028)] = "Cihaz sabotajı",          // Device Tampered
        [(2, 1024)] = "Cihaz işə düşdü",         // Device Powering On
    };

    // Yalnız minor ilə tanınanlar (major nəzərə alınmadan) — ehtiyat.
    private static readonly Dictionary<int, string> MinorOnly = new()
    {
        [75] = "Üz ilə təsdiqləndi",
        [76] = "Üz təsdiqi uğursuz",
    };

    private static readonly Dictionary<int, string> MajorFallback = new()
    {
        [1] = "Siqnal",
        [2] = "İstisna",
        [3] = "Əməliyyat",
        [5] = "Hadisə",
    };

    public static string Label(int? major, int? minor)
    {
        if (major is { } mj && minor is { } mn)
        {
            if (Map.TryGetValue((mj, mn), out var exact)) return exact;
            if (MinorOnly.TryGetValue(mn, out var byMinor)) return byMinor;
            if (MajorFallback.TryGetValue(mj, out var byMajor)) return $"{byMajor} ({mj}/{mn})";
            return $"Kod {mj}/{mn}";
        }
        if (minor is { } m2 && MinorOnly.TryGetValue(m2, out var only)) return only;
        return "Naməlum";
    }
}
