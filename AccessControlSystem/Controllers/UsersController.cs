using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class UsersController : Controller
{
    private readonly IUserService _users;
    private readonly IRoleService _roles;

    public UsersController(IUserService users, IRoleService roles)
    {
        _users = users;
        _roles = roles;
    }

    public async Task<IActionResult> Index()
    {
        var model = new UsersViewModel
        {
            Users = await _users.GetAllAsync(),
            Roles = await _roles.GetRoleOptionsAsync()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("users", PermType.Add)]
    public async Task<IActionResult> Create(string firstName, string lastName, string email, string password, long roleId)
    {
        await Guard(() => _users.CreateAsync(new UserCreateDto
        {
            FirstName = firstName, LastName = lastName, Email = email, Password = password, RoleId = roleId
        }), "İstifadəçi əlavə edildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("users", PermType.Edit)]
    public async Task<IActionResult> Update(long id, string firstName, string lastName, string email, long roleId, string? password)
    {
        await Guard(() => _users.UpdateAsync(id, new UserUpdateDto
        {
            FirstName = firstName, LastName = lastName, Email = email, RoleId = roleId, Password = password
        }), "İstifadəçi yeniləndi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("users", PermType.Edit)]
    public async Task<IActionResult> Toggle(long id)
    {
        await Guard(() => _users.ToggleStatusAsync(id), "İstifadəçinin statusu dəyişdirildi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("users", PermType.Delete)]
    public async Task<IActionResult> Delete(long id)
    {
        await Guard(() => _users.DeleteAsync(id), "İstifadəçi silindi.");
        return RedirectToAction(nameof(Index));
    }

    private async Task Guard(Func<Task> action, string success)
    {
        try { await action(); TempData["Success"] = success; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
    }
}
