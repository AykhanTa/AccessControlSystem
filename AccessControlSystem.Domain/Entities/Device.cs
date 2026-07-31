using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>
/// Fiziki Hikvision cihazı (bir mərtəbənin giriş və ya çıxış terminalı).
/// Kredit (admin login/parol) DB-də saxlanmır — bütün cihazlar üçün ortaqdır
/// və konfiqurasiyadan (Hikvision:Username/Password) götürülür.
/// </summary>
public class Device : BaseEntity
{
    public string Name { get; set; } = string.Empty;   // məs. "1-ci mərtəbə - Giriş"
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public bool UseHttps { get; set; } = false;
    public int DoorNo { get; set; } = 1;

    public long FloorId { get; set; }
    public Floor Floor { get; set; } = null!;

    /// <summary>Sahibi olan şirkət (çoxkiracılı təcrid).</summary>
    public long? CompanyId { get; set; }

    public DeviceDirection Direction { get; set; } = DeviceDirection.Entry;
    public bool IsActive { get; set; } = true;

    // Keçid nöqtəsi (məntiqi). Status məntiqi bunun PointType/Direction-una baxır (fallback: Device.Direction).
    public long? AccessPointId { get; set; }
    public AccessPoint? AccessPoint { get; set; }

    // Fiziki avadanlıq məlumatları (diaqrama uyğun)
    public string? SerialNo { get; set; }
    public string? Model { get; set; }
    public bool IsOnline { get; set; }
    public DateTime? LastSeenAt { get; set; }

    public ICollection<DeviceEnrollment> Enrollments { get; set; } = new List<DeviceEnrollment>();
}
