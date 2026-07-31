namespace AccessControlSystem.Application.DTOs;

/// <summary>Şöbə üzrə işçi giriş-çıxış hesabatı (çap üçün).</summary>
public class DeptAccessReportDto
{
    public string FromLabel { get; set; } = "";
    public string ToLabel { get; set; } = "";
    public string Scope { get; set; } = "Bütün şöbələr";
    public int TotalEmployees { get; set; }
    public int TotalEvents { get; set; }
    public List<DeptGroupDto> Groups { get; set; } = new();
}

public class DeptGroupDto
{
    public string Department { get; set; } = "";
    public string? Company { get; set; }
    public List<DeptEmployeeRowDto> Employees { get; set; } = new();
}

public class DeptEmployeeRowDto
{
    public string EmployeeNo { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Position { get; set; }
    public int EntryCount { get; set; }
    public int ExitCount { get; set; }
    public string? FirstIn { get; set; }
    public string? LastOut { get; set; }
    public int TotalEvents { get; set; }
    public List<DeptEventDto> Events { get; set; } = new();
}

public class DeptEventDto
{
    public string Time { get; set; } = "";
    public string? Point { get; set; }
    public string Direction { get; set; } = "entry";   // entry | exit
}
