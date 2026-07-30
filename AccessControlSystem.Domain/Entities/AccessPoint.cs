using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>
/// Keçid nöqtəsi — məntiqi giriş/çıxış yeri (turniket, qapı). Bir keçid nöqtəsinə
/// bir və ya bir neçə fiziki cihaz (Device) bağlana bilər. Mərtəbə + tip + istiqamət daşıyır.
/// </summary>
public class AccessPoint : BaseEntity
{
    public string Name { get; set; } = string.Empty;      // məs. "1-ci mərtəbə - Giriş"
    public long? CenterId { get; set; }
    public Center? Center { get; set; }

    public long FloorId { get; set; }
    public Floor Floor { get; set; } = null!;

    public PointType PointType { get; set; } = PointType.FloorEntrance;
    public DeviceDirection Direction { get; set; } = DeviceDirection.Entry;
    public bool IsActive { get; set; } = true;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
