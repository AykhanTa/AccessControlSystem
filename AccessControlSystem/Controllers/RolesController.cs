using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class RolesController : Controller
{
    private readonly IRoleService _roles;
    public RolesController(IRoleService roles) => _roles = roles;

    public async Task<IActionResult> Index()
    {
        var model = new RolesViewModel { Roles = await _roles.GetAllAsync() };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("roles", PermType.Add)]
    public async Task<IActionResult> Create(string name, string? description)
    {
        await Guard(() => _roles.CreateAsync(new RoleCreateDto { Name = name, Description = description }),
                    "Rol yaradıldı.");
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Bir rolun icazə matrisini bütövlükdə yadda saxlayır.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("roles", PermType.Edit)]
    public async Task<IActionResult> SavePermissions(long roleId)
    {
        var sectionIds = Request.Form["sectionIds"]
            .Where(v => long.TryParse(v, out _))
            .Select(long.Parse!)
            .ToList();

        var perms = sectionIds.Select(sid => new SectionPermDto
        {
            SectionId = sid,
            CanView = Request.Form.ContainsKey($"p_{sid}_view"),
            CanAdd = Request.Form.ContainsKey($"p_{sid}_add"),
            CanEdit = Request.Form.ContainsKey($"p_{sid}_edit"),
            CanDelete = Request.Form.ContainsKey($"p_{sid}_delete")
        }).ToList();

        await Guard(() => _roles.UpdatePermissionsAsync(roleId, perms), "İcazələr yadda saxlanıldı.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("roles", PermType.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        await Guard(() => _roles.DeleteAsync(id), "Rol silindi.");
        return RedirectToAction(nameof(Index));
    }

    private async Task Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
    }
}
