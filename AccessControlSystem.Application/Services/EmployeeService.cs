using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Application.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employees;
    private readonly ICompanyRepository _companies;
    private readonly IFloorRepository _floors;
    private readonly IUnitOfWork _uow;
    private readonly ISystemLogWriter _log;
    private readonly ICurrentTenant _tenant;

    public EmployeeService(IEmployeeRepository employees, ICompanyRepository companies,
        IFloorRepository floors, IUnitOfWork uow, ISystemLogWriter log, ICurrentTenant tenant)
    {
        _employees = employees;
        _companies = companies;
        _floors = floors;
        _uow = uow;
        _log = log;
        _tenant = tenant;
    }

    public async Task<List<EmployeeRowDto>> GetAllAsync(CancellationToken ct = default) =>
        (await _employees.GetAllAsync(ct)).Select(e => new EmployeeRowDto
        {
            Id = e.Id,
            EmployeeNo = e.EmployeeNo,
            Name = e.FullName,
            Photo = e.PhotoPath,
            Company = e.Company?.Name ?? "",
            Department = e.Department?.Name,
            Position = e.Position?.Name,
            Phone = e.Phone,
            Floors = string.Join(", ", e.EmployeeFloors.Select(f => f.Floor.Name)),
            Status = e.Status.ToString().ToLowerInvariant(),
            FaceStatus = e.FaceStatus.ToString().ToLowerInvariant(),
            Presence = e.CurrentPresence.ToString().ToLowerInvariant(),
            LastSeen = e.LastSeenAt?.ToString("dd.MM HH:mm"),
            FirstName = e.FirstName,
            LastName = e.LastName,
            Patronymic = e.Patronymic,
            FinCode = e.FinCode,
            DocumentNo = e.DocumentNo,
            Email = e.Email,
            CompanyId = e.CompanyId,
            DepartmentId = e.DepartmentId,
            PositionId = e.PositionId,
            EmploymentStartAt = e.EmploymentStartAt?.ToString("yyyy-MM-dd"),
            DeviceNumbers = e.DeviceNumbers,
            DeviceName = e.DeviceName,
            FloorIds = e.EmployeeFloors.Select(f => f.FloorId).ToList()
        }).ToList();

    public async Task<List<EmployeePresenceDto>> GetPresencesAsync(CancellationToken ct = default) =>
        (await _employees.GetPresencePairsAsync(ct)).Select(p => new EmployeePresenceDto
        {
            Id = p.Id,
            Presence = p.Presence.ToString().ToLowerInvariant(),
            LastSeen = p.LastSeen?.ToString("dd.MM HH:mm")
        }).ToList();

    public async Task<long> CreateAsync(EmployeeCreateDto dto, CancellationToken ct = default)
    {
        // Şirkət istifadəçisi yalnız öz şirkətinə işçi əlavə edə bilər (formdakı seçim iqnor edilir).
        if (!_tenant.IsGlobalAdmin && _tenant.CompanyId is { } cid)
            dto.CompanyId = cid;

        Validate(dto);
        if (await _companies.GetByIdAsync(dto.CompanyId, ct) is null)
            throw new ArgumentException("Şirkət seçilməlidir.");

        var employeeNo = string.IsNullOrWhiteSpace(dto.EmployeeNo)
            ? await GenerateEmployeeNoAsync(ct)
            : dto.EmployeeNo.Trim();
        if (await _employees.ExistsByEmployeeNoAsync(employeeNo, null, ct))
            throw new ArgumentException("Bu İşçi İD-si artıq mövcuddur.");

        var employee = new Employee
        {
            EmployeeNo = employeeNo,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            Patronymic = dto.Patronymic?.Trim(),
            FinCode = dto.FinCode?.Trim(),
            DocumentNo = dto.DocumentNo?.Trim(),
            Phone = dto.Phone?.Trim(),
            Email = dto.Email?.Trim(),
            PhotoPath = dto.PhotoPath,
            CompanyId = dto.CompanyId,
            DepartmentId = dto.DepartmentId,
            PositionId = dto.PositionId,
            EmploymentStartAt = dto.EmploymentStartAt,
            DeviceNumbers = NormalizeDeviceNumbers(dto.DeviceNumbers),
            DeviceName = string.IsNullOrWhiteSpace(dto.DeviceName) ? null : dto.DeviceName.Trim(),
            Status = EmployeeStatus.Active,
            FaceStatus = FaceStatus.None
        };
        foreach (var floor in await _floors.GetByIdsAsync(dto.FloorIds, ct))
            employee.EmployeeFloors.Add(new EmployeeFloor { Floor = floor });

        await _employees.AddAsync(employee, ct);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("EMPLOYEE_CREATED", $"{employee.FullName} işçisi əlavə edildi.", "employee", employee.Id, ct: ct);
        return employee.Id;
    }

    public async Task UpdateAsync(long id, EmployeeCreateDto dto, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(id, ct)
                       ?? throw new KeyNotFoundException("İşçi tapılmadı.");
        if (!_tenant.IsGlobalAdmin && _tenant.CompanyId is { } cid)
            dto.CompanyId = cid;   // şirkət istifadəçisi işçini başqa şirkətə keçirə bilməz
        Validate(dto);
        if (await _companies.GetByIdAsync(dto.CompanyId, ct) is null)
            throw new ArgumentException("Şirkət seçilməlidir.");
        if (!string.IsNullOrWhiteSpace(dto.EmployeeNo))
        {
            if (await _employees.ExistsByEmployeeNoAsync(dto.EmployeeNo.Trim(), id, ct))
                throw new ArgumentException("Bu İşçi İD-si artıq mövcuddur.");
            employee.EmployeeNo = dto.EmployeeNo.Trim();
        }

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.Patronymic = dto.Patronymic?.Trim();
        employee.FinCode = dto.FinCode?.Trim();
        employee.DocumentNo = dto.DocumentNo?.Trim();
        employee.Phone = dto.Phone?.Trim();
        employee.Email = dto.Email?.Trim();
        if (!string.IsNullOrWhiteSpace(dto.PhotoPath)) employee.PhotoPath = dto.PhotoPath;
        employee.CompanyId = dto.CompanyId;
        employee.DepartmentId = dto.DepartmentId;
        employee.PositionId = dto.PositionId;
        employee.EmploymentStartAt = dto.EmploymentStartAt;
        employee.DeviceNumbers = NormalizeDeviceNumbers(dto.DeviceNumbers);
        employee.DeviceName = string.IsNullOrWhiteSpace(dto.DeviceName) ? null : dto.DeviceName.Trim();
        employee.UpdatedAt = DateTime.Now;

        // İcazəli mərtəbələri yenilə
        employee.EmployeeFloors.Clear();
        foreach (var floor in await _floors.GetByIdsAsync(dto.FloorIds, ct))
            employee.EmployeeFloors.Add(new EmployeeFloor { Floor = floor });

        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("EMPLOYEE_UPDATED", $"{employee.FullName} işçisi yeniləndi.", "employee", employee.Id, ct: ct);
    }

    public async Task ToggleStatusAsync(long id, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(id, ct)
                       ?? throw new KeyNotFoundException("İşçi tapılmadı.");
        employee.Status = employee.Status == EmployeeStatus.Active ? EmployeeStatus.Inactive : EmployeeStatus.Active;
        employee.UpdatedAt = DateTime.Now;
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("EMPLOYEE_STATUS_CHANGED",
            $"{employee.FullName} işçisi {(employee.Status == EmployeeStatus.Active ? "aktiv" : "deaktiv")} edildi.",
            "employee", employee.Id, ct: ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var employee = await _employees.GetByIdAsync(id, ct)
                       ?? throw new KeyNotFoundException("İşçi tapılmadı.");
        var name = employee.FullName;
        _employees.Remove(employee);
        await _uow.SaveChangesAsync(ct);
        await _log.LogAsync("EMPLOYEE_DELETED", $"{name} işçisi silindi.", "employee", id, ct: ct);
    }

    private static void Validate(EmployeeCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FirstName) || string.IsNullOrWhiteSpace(dto.LastName))
            throw new ArgumentException("Ad və Soyad mütləq doldurulmalıdır.");
        if (dto.CompanyId <= 0)
            throw new ArgumentException("Şirkət seçilməlidir.");
    }

    /// <summary>Cihaz alias sətrini təmizləyir ("cihazId:nömrə,..." — formu JS qurur; boşdursa null).</summary>
    private static string? NormalizeDeviceNumbers(string? input) =>
        string.IsNullOrWhiteSpace(input) ? null : input.Trim();

    private async Task<string> GenerateEmployeeNoAsync(CancellationToken ct)
    {
        var count = await _employees.CountAsync(ct);
        string no;
        do { no = $"EMP-{++count:D4}"; }
        while (await _employees.ExistsByEmployeeNoAsync(no, null, ct));
        return no;
    }
}
