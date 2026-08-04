namespace AccessControlSystem.Application.Interfaces.Services;

/// <summary>
/// Cari sorğunun şirkət konteksti (çoxkiracılı təcrid üçün). AppDbContext-in global
/// query filter-ləri bundan istifadə edir. HTTP konteksti yoxdursa (background servis,
/// seeding, login) və ya qlobal admindirsə → IsGlobalAdmin=true (filtr tətbiq olunmur).
/// </summary>
public interface ICurrentTenant
{
    /// <summary>Yazma səlahiyyəti üçün — yalnız qorunan qlobal admin (şirkət/mərkəz yaradır və s.).</summary>
    bool IsGlobalAdmin { get; }
    /// <summary>OXU görünüşü üçün — qlobal admin VƏ YA təhlükəsizlik məsulu (bütün müəssisələri görür).</summary>
    bool CanSeeAllCompanies { get; }
    long? CompanyId { get; }
}
