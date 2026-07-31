namespace AccessControlSystem.Domain.Entities;

/// <summary>Sistem audit loqu — kim, nə vaxt, hansı əməliyyatı etdi.</summary>
public class SystemLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }                    // əməliyyatı edən istifadəçi (varsa)
    public string UserName { get; set; } = "Sistem";     // görünən ad (denormallaşdırılmış, "Sistem" default)
    public string Action { get; set; } = string.Empty;   // 'CARD_CREATED', 'ROLE_PERMISSION_UPDATED' ...
    public string? EntityType { get; set; }              // 'card','user','role' ...
    public long? EntityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    /// <summary>Əməliyyatı edən istifadəçinin şirkəti (təcrid). null = qlobal/sistem.</summary>
    public long? CompanyId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
