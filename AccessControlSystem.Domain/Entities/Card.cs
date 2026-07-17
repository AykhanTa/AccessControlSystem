using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Müvəqqəti giriş kartı.</summary>
public class Card : BaseEntity
{
    public string CardNo { get; set; } = string.Empty;   // məs. 'MK-24-00087'
    public string? Note { get; set; }
    public CardStatus Status { get; set; } = CardStatus.Free;
    public bool IsActive { get; set; } = true;            // false = deaktiv edilmiş kart

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
