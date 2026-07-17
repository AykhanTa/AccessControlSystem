using AccessControlSystem.Domain.Common;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Qonaq (şəxs). Təkrar ziyarətlərdə eyni qonaq istifadə olunur.</summary>
public class Guest : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;   // Ad
    public string LastName { get; set; } = string.Empty;    // Soyad
    public string? Patronymic { get; set; }                 // Atasının adı
    public string IdDocument { get; set; } = string.Empty;  // Şəxsiyyət vəsiqəsi
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoPath { get; set; }                  // yüklənmiş foto
    public string? DocumentPath { get; set; }               // əlavə sənəd faylı

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public string FullName => $"{FirstName} {LastName}".Trim();
}
