using AccessControlSystem.Application.Common;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;

namespace AccessControlSystem.Application.Services;

/// <summary>Audit loqlarını yazır (best-effort — loqlama xətası əsas əməliyyatı pozmur).</summary>
public class SystemLogWriter : ISystemLogWriter
{
    private readonly ISystemLogRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ICurrentTenant _tenant;

    public SystemLogWriter(ISystemLogRepository repo, IUnitOfWork uow, ICurrentUserService currentUser, ICurrentTenant tenant)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _tenant = tenant;
    }

    public async Task LogAsync(string action, string description, string? entityType = null, long? entityId = null,
                               long? actorUserId = null, string? actorName = null, CancellationToken ct = default)
    {
        try
        {
            var log = new SystemLog
            {
                Action = action,
                Description = description,
                EntityType = entityType,
                EntityId = entityId,
                UserId = actorUserId ?? _currentUser.UserId,
                UserName = !string.IsNullOrWhiteSpace(actorName) ? actorName! : _currentUser.UserName,
                IpAddress = _currentUser.IpAddress,
                CompanyId = _tenant.CompanyId,   // qlobal/sistem → null (yalnız qlobal admin görür)
                CreatedAt = DateTime.Now
            };
            await _repo.AddAsync(log, ct);
            await _uow.SaveChangesAsync(ct);
        }
        catch
        {
            // Audit loqu yazıla bilmədisə əsas əməliyyat dayanmasın.
        }
    }
}

public class SystemLogService : ISystemLogService
{
    private readonly ISystemLogRepository _repo;
    public SystemLogService(ISystemLogRepository repo) => _repo = repo;

    public async Task<PagedLogsDto> GetPagedAsync(string? search, int page, int pageSize, CancellationToken ct = default)
    {
        if (pageSize < 1) pageSize = 20;
        var total = await _repo.CountAsync(search, ct);
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        if (page < 1) page = 1;
        if (page > totalPages) page = totalPages;

        var logs = await _repo.GetPagedAsync(search, (page - 1) * pageSize, pageSize, ct);
        return new PagedLogsDto
        {
            Total = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = totalPages,
            Items = logs.Select(l => new LogDto
            {
                DateTime = AzDate.Format(l.CreatedAt) ?? string.Empty,
                Action = l.Action,
                Content = l.Description,
                PerformedBy = string.IsNullOrWhiteSpace(l.UserName) ? "Sistem" : l.UserName
            }).ToList()
        };
    }
}
