namespace AccessControlSystem.Domain.Common;

/// <summary>Bütün entity-lər üçün ortaq baza: Id və yaradılma/yenilənmə tarixləri.</summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
