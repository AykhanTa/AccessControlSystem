using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class LogsController : Controller
{
    private const int PageSize = 20;
    private readonly ISystemLogService _logs;
    public LogsController(ISystemLogService logs) => _logs = logs;

    /// <summary>Sistem audit loqları — axtarış + səhifələmə (20/səhifə). print=1 olduqda axtarışa uyğun bütün nəticələr çap üçün.</summary>
    public async Task<IActionResult> Index(string? q, int page = 1, int print = 0)
    {
        if (print == 1)
        {
            var all = await _logs.GetPagedAsync(q, 1, int.MaxValue);
            return View(new LogsViewModel { Logs = all.Items, Total = all.Total, Query = q, PrintMode = true });
        }

        var result = await _logs.GetPagedAsync(q, page, PageSize);
        return View(new LogsViewModel
        {
            Logs = result.Items,
            Total = result.Total,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            Query = q
        });
    }
}
