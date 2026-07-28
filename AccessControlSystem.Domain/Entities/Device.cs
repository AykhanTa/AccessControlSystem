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

    public DeviceDirection Direction { get; set; } = DeviceDirection.Entry;
    public bool IsActive { get; set; } = true;

    public ICollection<DeviceEnrollment> Enrollments { get; set; } = new List<DeviceEnrollment>();
}
