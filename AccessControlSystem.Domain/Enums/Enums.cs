namespace AccessControlSystem.Domain.Enums;

/// <summary>Müvəqqəti kartın vəziyyəti.</summary>
public enum CardStatus
{
    /// <summary>Boş — istifadəyə hazır.</summary>
    Free = 0,
    /// <summary>Qonağa təyin edilib.</summary>
    Assigned = 1
}

/// <summary>Buraxılış növü.</summary>
public enum PassType
{
    /// <summary>Müvəqqəti kartla buraxılış.</summary>
    Card = 0,
    /// <summary>QR kod ilə buraxılış.</summary>
    Qr = 1
}

/// <summary>Sistem istifadəçisinin statusu.</summary>
public enum UserStatus
{
    Active = 0,
    Inactive = 1
}

/// <summary>Ziyarətin statusu (həyat dövrü).</summary>
public enum VisitStatus
{
    /// <summary>Binadadır (giriş cihazına oxudub keçib).</summary>
    In = 0,
    /// <summary>Çıxıb.</summary>
    Out = 1,
    /// <summary>Gecikib.</summary>
    Late = 2,
    /// <summary>Planlaşdırılmış — əvvəlcədən daxil edilib, hələ gəlməyib (kart yox, cihazda yox).</summary>
    Planned = 3,
    /// <summary>Kart verilib — nəzarətçi kart təyin edib, cihazlara yazılıb, giriş gözlənilir.</summary>
    CheckedIn = 4,
    /// <summary>Mərtəbədədir — mərtəbə giriş cihazına oxudub.</summary>
    OnFloor = 5
}

/// <summary>Cihazın/keçid nöqtəsinin istiqaməti.</summary>
public enum DeviceDirection
{
    /// <summary>Giriş.</summary>
    Entry = 0,
    /// <summary>Çıxış.</summary>
    Exit = 1
}

/// <summary>Keçid nöqtəsinin tipi — status məntiqi bundan asılıdır.</summary>
public enum PointType
{
    /// <summary>Binanın əsas girişi → Binadadır.</summary>
    MainEntrance = 0,
    /// <summary>Binanın əsas çıxışı → Çıxıb.</summary>
    MainExit = 1,
    /// <summary>Mərtəbə girişi → Mərtəbədə.</summary>
    FloorEntrance = 2,
    /// <summary>Mərtəbə çıxışı.</summary>
    FloorExit = 3,
    /// <summary>Adi qapı.</summary>
    Door = 4,
    /// <summary>Turniket (əsas giriş kimi).</summary>
    Turnstile = 5
}

/// <summary>İşçinin statusu.</summary>
public enum EmployeeStatus
{
    Active = 0,
    Inactive = 1,
    Terminated = 2
}

/// <summary>İşçinin üz məlumatının cihazlara sinxronizasiya vəziyyəti.</summary>
public enum FaceStatus
{
    None = 0,        // üz yoxdur
    Pending = 1,     // yüklənib, cihaza göndəriləcək
    Synced = 2,      // cihazlara yazılıb
    Failed = 3       // yükləmə uğursuz
}

/// <summary>İşçinin hazırkı mövqeyi (keçid hadisələrindən).</summary>
public enum PresenceStatus
{
    Out = 0,         // bayırda
    In = 1,          // binadadır
    OnFloor = 2      // mərtəbədədir
}

/// <summary>Bir ziyarətin bir cihaza sinxronizasiya vəziyyəti.</summary>
public enum EnrollmentStatus
{
    /// <summary>Gözləyir — hələ cihaza yazılmayıb.</summary>
    Pending = 0,
    /// <summary>Cihaza uğurla yazılıb.</summary>
    Synced = 1,
    /// <summary>Yazma uğursuz oldu (retry lazımdır).</summary>
    Failed = 2,
    /// <summary>Cihazdan silinib.</summary>
    Revoked = 3
}
