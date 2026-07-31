namespace AccessControlSystem.Application.DTOs;

public class HostItemDto
{
    public long Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
    public bool IsActive { get; set; }
}

public class HostInputDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Department { get; set; }
}

public class AreaItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

public class PurposeItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CenterItemDto
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? City { get; set; }
    public bool IsActive { get; set; }
    public int FloorCount { get; set; }
}

public class CenterInputDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    /// <summary>Sahibi olan şirkət (qlobal admin təyin edir).</summary>
    public long? CompanyId { get; set; }
}

public class FloorItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DeviceCount { get; set; }
    public long? CenterId { get; set; }
    public string? CenterName { get; set; }
}

// ---------- Təşkilati struktur ----------

public class CompanyItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public int DepartmentCount { get; set; }
}

public class CompanyInputDto
{
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class DepartmentItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public bool IsActive { get; set; }
}

public class PositionItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class DeviceItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; }
    public bool UseHttps { get; set; }
    public int DoorNo { get; set; }
    public long FloorId { get; set; }
    public string FloorName { get; set; } = string.Empty;
    public string Direction { get; set; } = "Entry";   // "Entry" | "Exit"
    public string PointType { get; set; } = "Door";    // keçid nöqtəsinin tipi
    public bool IsActive { get; set; }
}

public class DeviceInputDto
{
    public string Name { get; set; } = string.Empty;
    public string Ip { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public bool UseHttps { get; set; }
    public int DoorNo { get; set; } = 1;
    public long FloorId { get; set; }
    public string Direction { get; set; } = "Entry";   // "Entry" | "Exit"
    public string PointType { get; set; } = "Door";    // MainEntrance/FloorEntrance/Door/Turnstile...
}
