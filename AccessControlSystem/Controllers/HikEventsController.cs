using System.Text.Json;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessControlSystem.Controllers;

/// <summary>
/// Hikvision cihazlarının real-vaxt keçid hadisələrini qəbul edir (httpHosts).
/// Cihaz autentifikasiyasız POST edir, ona görə [AllowAnonymous]. Həmişə 200 qaytarır
/// ki, cihaz təkrar göndərmə (retry storm) etməsin.
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("api/hik")]
public class HikEventsController : ControllerBase
{
    private readonly IVisitEventService _events;
    private readonly ILogger<HikEventsController> _logger;

    // Cihazdan gələn "icazə verilmədi" (deny) subEventType kodları.
    // 75 = Legal Card Authenticated (granted). 76 = authentication failed (deny) — real payloadda görüldü.
    private static readonly HashSet<int> DenyMinorTypes = new() { 76 };

    public HikEventsController(IVisitEventService events, ILogger<HikEventsController> logger)
    {
        _events = events;
        _logger = logger;
    }

    [HttpPost("events")]
    public async Task<IActionResult> Events(CancellationToken ct)
    {
        try
        {
            var json = await ExtractJsonAsync();
            var remoteIp = NormalizeIp(HttpContext.Connection.RemoteIpAddress?.ToString());
            var dto = ParseEvent(json, remoteIp);

            // Diaqnostika: gələn HƏR POST-u izlə (saxlanmasa belə) — backlog vs canlı.
            Services.HikEventMonitor.Record(dto.AccessNumber, dto.Granted, dto.DeviceTime, dto.MinorType);

            // Şəxssiz (qapı sensoru / hardware) hadisələri erkən süz.
            // Qalan filtr (yalnız bizim qonaq) VisitEventService-dədir — cihaz saatından asılı deyil.
            if (string.IsNullOrEmpty(dto.AccessNumber))
                return Ok();

            await _events.ProcessAsync(dto, ct);
        }
        catch (Exception ex)
        {
            // Cihaz təkrar göndərməsin deyə xətanı udur, sadəcə loglayırıq.
            _logger.LogWarning(ex, "Hikvision event emalı xətası");
        }
        return Ok();
    }

    private async Task<string?> ExtractJsonAsync()
    {
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            foreach (var kv in form)
            {
                var v = kv.Value.ToString();
                if (!string.IsNullOrWhiteSpace(v) && v.TrimStart().StartsWith("{"))
                    return v;
            }
            foreach (var file in form.Files)
            {
                var isJson = (file.ContentType?.Contains("json", StringComparison.OrdinalIgnoreCase) ?? false)
                             || file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
                if (isJson)
                {
                    using var sr = new StreamReader(file.OpenReadStream());
                    return await sr.ReadToEndAsync();
                }
            }
            return null;
        }

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        return string.IsNullOrWhiteSpace(body) ? null : body;
    }

    private static HikEventDto ParseEvent(string? json, string? remoteIp)
    {
        var dto = new HikEventDto { DeviceIp = remoteIp, OccurredAt = DateTime.Now, Raw = json };
        if (string.IsNullOrWhiteSpace(json))
            return dto;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("ipAddress", out var ip) && ip.ValueKind == JsonValueKind.String)
                dto.DeviceIp = ip.GetString() ?? remoteIp;

            if (root.TryGetProperty("dateTime", out var dtEl) && dtEl.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(dtEl.GetString(), out var devTime))
            {
                dto.DeviceTime = devTime;
                dto.OccurredAt = devTime.LocalDateTime;
            }

            if (root.TryGetProperty("AccessControllerEvent", out var ace) && ace.ValueKind == JsonValueKind.Object)
            {
                dto.AccessNumber = GetStr(ace, "employeeNoString") ?? GetStr(ace, "cardNo");
                dto.PersonName = GetStr(ace, "name");
                dto.MajorType = GetInt(ace, "majorEventType");
                dto.MinorType = GetInt(ace, "subEventType");
            }
            // Ehtiyat: bəzən sahələr kökdə olur.
            dto.AccessNumber ??= GetStr(root, "employeeNoString") ?? GetStr(root, "cardNo");
        }
        catch (JsonException)
        {
            // Parse olunmasa da xam saxlanılır (audit).
        }

        dto.Granted = !string.IsNullOrEmpty(dto.AccessNumber)
                      && (dto.MinorType is null || !DenyMinorTypes.Contains(dto.MinorType.Value));
        return dto;
    }

    private static string? GetStr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static int? GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : null;

    private static string? NormalizeIp(string? ip) =>
        ip is not null && ip.StartsWith("::ffff:") ? ip[7..] : ip;
}
