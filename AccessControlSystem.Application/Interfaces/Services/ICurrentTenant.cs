namespace AccessControlSystem.Application.Interfaces.Services;

/// <summary>
/// Cari sorğunun şirkət konteksti (çoxkiracılı təcrid üçün). AppDbContext-in global
/// query filter-ləri bundan istifadə edir. HTTP konteksti yoxdursa (background servis,
/// seeding, login) və ya qlobal admindirsə → IsGlobalAdmin=true (filtr tətbiq olunmur).
/// </summary>
public interface ICurrentTenant
{
    bool IsGlobalAdmin { get; }
    long? CompanyId { get; }
}
