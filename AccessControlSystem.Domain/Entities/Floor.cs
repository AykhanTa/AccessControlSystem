using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Mərtəbə — giriş nəzarətinin əsas vahidi. Hər mərtəbənin öz cihaz(lar)ı var.</summary>
public class Floor : BaseEntity
{
    public string Name { get; set; } = string.Empty;   // məs. "1-ci mərtəbə"
    public int? FloorNumber { get; set; }               // mərtəbə nömrəsi (opsional)
    public bool IsActive { get; set; } = true;

    public long? CenterId { get; set; }                 // hansı mərkəzə aiddir
    public Center? Center { get; set; }

    /// <summary>Sahibi olan şirkət (çoxkiracılı təcrid).</summary>
    public long? CompanyId { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<VisitFloor> VisitFloors { get; set; } = new List<VisitFloor>();
}
