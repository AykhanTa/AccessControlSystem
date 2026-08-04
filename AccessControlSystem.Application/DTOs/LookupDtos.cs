namespace AccessControlSystem.Application.DTOs;

/// <summary>Açılan siyahılar (dropdown) üçün sadə ad-dəyər cütü.</summary>
public class LookupDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Şirkət konteksti (məs. host şirkəti) — kaskad süzgəc üçün. Opsional.</summary>
    public long? CompanyId { get; set; }
}
