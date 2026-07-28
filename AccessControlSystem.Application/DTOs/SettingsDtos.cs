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

public class FloorItemDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int DeviceCount { get; set; }
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
}
