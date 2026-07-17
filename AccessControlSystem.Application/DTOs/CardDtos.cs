namespace AccessControlSystem.Application.DTOs;

/// <summary>Kart cədvəlinin bir sətri.</summary>
public class CardDto
{
    public long Id { get; set; }
    public string No { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string Status { get; set; } = "free";   // "free" | "assigned"
    public string? AssignedTo { get; set; }          // təyin edilmiş qonağın adı
    public bool Active { get; set; } = true;
}

public class CardCreateDto
{
    public string No { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public class CardUpdateDto
{
    public string No { get; set; } = string.Empty;
    public string? Note { get; set; }
}
