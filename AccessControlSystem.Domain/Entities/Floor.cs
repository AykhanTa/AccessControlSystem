using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Mərtəbə — giriş nəzarətinin əsas vahidi. Hər mərtəbənin öz cihaz(lar)ı var.</summary>
public class Floor : BaseEntity
{
    public string Name { get; set; } = string.Empty;   // məs. "1-ci mərtəbə"
    public bool IsActive { get; set; } = true;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
    public ICollection<VisitFloor> VisitFloors { get; set; } = new List<VisitFloor>();
}
