using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Filters;
using AccessControlSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

public class GuestsController : Controller
{
    private readonly IGuestService _guests;
    private readonly ILookupService _lookups;
    private readonly IWebHostEnvironment _env;

    private static readonly string[] AllowedExtensions =
        { ".jpg", ".jpeg", ".png", ".webp", ".gif", ".pdf" };

    public GuestsController(IGuestService guests, ILookupService lookups, IWebHostEnvironment env)
    {
        _guests = guests;
        _lookups = lookups;
        _env = env;
    }

    /// <summary>Qonaq reyestri + filtr/modal üçün açılan siyahılar.</summary>
    public async Task<IActionResult> Index()
    {
        var model = await BuildViewModelAsync();
        return View(model);
    }

    /// <summary>Yeni qonaq + ziyarət qeydiyyatı (modal formu).</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("guests", PermType.Add)]
    public async Task<IActionResult> Create(GuestFormModel form)
    {
        try
        {
            var arrivalAt = CombineDateTime(form.ArrivalDate, form.ArrivalTime) ?? DateTime.Today;
            DateTime? exitAt = form.ExitDate is null ? null : CombineDateTime(form.ExitDate.Value, form.ExitTime);

            var dto = new GuestCreateDto
            {
                FirstName = form.FirstName,
                LastName = form.LastName,
                Patronymic = form.Patronymic,
                IdDocument = form.IdDocument,
                Phone = form.Phone,
                Email = form.Email,
                PhotoPath = await SaveFileAsync(form.Photo, "guests"),
                DocumentPath = await SaveFileAsync(form.Document, "documents"),
                HostId = form.HostId,
                ArrivalAt = arrivalAt,
                ExpectedExitAt = exitAt,
                PassType = form.PassType,
                CardId = form.PassType == "card" ? form.CardId : null,
                AreaIds = form.AreaId > 0 ? new List<long> { form.AreaId } : new(),
                PurposeIds = form.PurposeId > 0 ? new List<long> { form.PurposeId } : new(),
                Note = form.Note
            };

            await _guests.RegisterAsync(dto);
            TempData["Success"] = $"{form.FirstName} {form.LastName} uğurla qeydiyyata alındı.";
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Ziyarətin çıxışını təsdiqlə.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequirePermission("guests", PermType.Edit)]
    public async Task<IActionResult> Checkout(long id)
    {
        try
        {
            await _guests.CheckOutAsync(id);
            TempData["Success"] = "Çıxış təsdiqləndi.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<GuestsViewModel> BuildViewModelAsync() => new()
    {
        Guests = await _guests.GetRegistryAsync(),
        Hosts = await _lookups.GetHostsAsync(),
        Areas = await _lookups.GetAreasAsync(),
        Purposes = await _lookups.GetPurposesAsync(),
        FreeCards = await _lookups.GetFreeCardsAsync()
    };

    private static DateTime? CombineDateTime(DateTime date, string? time)
    {
        if (TimeSpan.TryParse(time, out var t)) return date.Date + t;
        return date.Date;
    }

    /// <summary>Yüklənmiş faylı wwwroot/uploads/{kind}-a yazır, nisbi yolu qaytarır.</summary>
    private async Task<string?> SaveFileAsync(IFormFile? file, string kind)
    {
        if (file is null || file.Length == 0) return null;
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            throw new ArgumentException("Yalnız şəkil (jpg, png, webp, gif) və ya PDF faylı yükləmək olar.");

        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var dir = Path.Combine(webRoot, "uploads", kind);
        Directory.CreateDirectory(dir);
        var name = $"{Guid.NewGuid():N}{ext}";
        await using var stream = System.IO.File.Create(Path.Combine(dir, name));
        await file.CopyToAsync(stream);
        return $"/uploads/{kind}/{name}";
    }
}
