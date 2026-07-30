using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AccessControlSystem.Application.DTOs;
using AccessControlSystem.Application.Interfaces.Services;

namespace AccessControlSystem.Infrastructure.Hikvision;

/// <summary>
/// Hikvision ISAPI ilə danışan servis. Digest authentication .NET-in
/// <see cref="HttpClientHandler"/>-i tərəfindən avtomatik idarə olunur
/// (401 → WWW-Authenticate: Digest → təkrar sorğu). HttpClient hər cihaz
/// üçün keşlənir (socket tükənməsinin qarşısını almaq üçün).
/// </summary>
public class HikvisionDeviceService : IHikvisionDeviceService
{
    private static readonly ConcurrentDictionary<string, HttpClient> _clients = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = null // Hikvision sahə adları dəqiq yazılışla göndərilir
    };

    private HttpClient GetClient(HikDevice device)
    {
        return _clients.GetOrAdd(device.Key, _ =>
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(device.Username, device.Password),
                PreAuthenticate = true
            };
            if (device.UseHttps)
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;

            return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
        });
    }

    // ---- Aşağı səviyyəli ISAPI göndərmə ----

    private async Task<HikResult> SendAsync(
        HikDevice device, HttpMethod method, string path,
        string? body, string contentType, CancellationToken ct)
    {
        var client = GetClient(device);
        var url = device.BaseUrl + path;

        try
        {
            using var req = new HttpRequestMessage(method, url);
            if (body is not null)
                req.Content = new StringContent(body, Encoding.UTF8, contentType);

            using var resp = await client.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            return Parse((int)resp.StatusCode, resp.IsSuccessStatusCode, raw);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return HikResult.Fail($"Vaxt bitdi (cihaz {device.Ip} cavab vermir).");
        }
        catch (HttpRequestException ex)
        {
            return HikResult.Fail($"Bağlantı xətası: {ex.Message}");
        }
    }

    /// <summary>Hikvision cavabını (JSON statusCode/errorMsg) HikResult-a çevirir.</summary>
    private static HikResult Parse(int httpStatus, bool httpOk, string raw)
    {
        int? statusCode = null;
        string? subStatus = null;
        string? errorMsg = null;

        if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith("{"))
        {
            try
            {
                using var doc = JsonDocument.Parse(raw);
                var root = doc.RootElement;
                if (root.TryGetProperty("statusCode", out var sc) && sc.ValueKind == JsonValueKind.Number)
                    statusCode = sc.GetInt32();
                if (root.TryGetProperty("subStatusCode", out var ssc))
                    subStatus = ssc.GetString();
                if (root.TryGetProperty("errorMsg", out var em))
                    errorMsg = em.GetString();
            }
            catch (JsonException) { /* JSON deyilsə xam mətnlə qalırıq */ }
        }

        // Uğur: HTTP 2xx VƏ (statusCode yoxdur və ya == 1). Hikvision-da 1 = OK.
        var success = httpOk && (statusCode is null || statusCode == 1);
        return new HikResult(success, httpStatus, statusCode, subStatus, errorMsg, raw);
    }

    private Task<HikResult> PostJson(HikDevice d, string path, object payload, CancellationToken ct) =>
        SendAsync(d, HttpMethod.Post, path, JsonSerializer.Serialize(payload, JsonOpts), "application/json", ct);

    private Task<HikResult> PutJson(HikDevice d, string path, object payload, CancellationToken ct) =>
        SendAsync(d, HttpMethod.Put, path, JsonSerializer.Serialize(payload, JsonOpts), "application/json", ct);

    private static string Iso(DateTime t) => t.ToString("yyyy-MM-ddTHH:mm:ss");

    // ---- Yüksək səviyyəli əməliyyatlar ----

    public Task<HikResult> TestConnectionAsync(HikDevice device, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Get, "/ISAPI/System/deviceInfo", null, "application/xml", ct);

    public Task<HikResult> CreateUserAsync(HikDevice device, HikUser user, CancellationToken ct = default) =>
        PostJson(device, "/ISAPI/AccessControl/UserInfo/Record?format=json", UserPayload(user), ct);

    public Task<HikResult> UpdateUserAsync(HikDevice device, HikUser user, CancellationToken ct = default) =>
        PutJson(device, "/ISAPI/AccessControl/UserInfo/Modify?format=json", UserPayload(user), ct);

    private static object UserPayload(HikUser u) => new
    {
        UserInfo = new
        {
            employeeNo = u.EmployeeNo,
            name = u.Name,
            userType = "normal",
            Valid = new
            {
                enable = true,
                beginTime = Iso(u.BeginTime),
                endTime = Iso(u.EndTime),
                timeType = "local"
            },
            doorRight = u.DoorNo.ToString(),
            RightPlan = new[] { new { doorNo = u.DoorNo, planTemplateNo = u.PlanTemplateNo } }
        }
    };

    public Task<HikResult> BindCardAsync(HikDevice device, string employeeNo, string cardNo, CancellationToken ct = default) =>
        PostJson(device, "/ISAPI/AccessControl/CardInfo/Record?format=json", new
        {
            CardInfo = new { employeeNo, cardNo, cardType = "normalCard" }
        }, ct);

    public Task<HikResult> DeleteUserAsync(HikDevice device, string employeeNo, CancellationToken ct = default) =>
        PutJson(device, "/ISAPI/AccessControl/UserInfo/Delete?format=json", new
        {
            UserInfoDelCond = new { EmployeeNoList = new[] { new { employeeNo } } }
        }, ct);

    public Task<HikResult> DeleteCardAsync(HikDevice device, string cardNo, CancellationToken ct = default) =>
        PutJson(device, "/ISAPI/AccessControl/CardInfo/Delete?format=json", new
        {
            CardInfoDelCond = new { CardNoList = new[] { new { cardNo } } }
        }, ct);

    public Task<HikResult> SearchUserAsync(HikDevice device, string employeeNo, CancellationToken ct = default) =>
        PostJson(device, "/ISAPI/AccessControl/UserInfo/Search?format=json", new
        {
            UserInfoSearchCond = new
            {
                searchID = "1",
                searchResultPosition = 0,
                maxResults = 30,
                EmployeeNoList = new[] { new { employeeNo } }
            }
        }, ct);

    public Task<HikResult> RemoteOpenDoorAsync(HikDevice device, int doorNo = 1, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Put, $"/ISAPI/AccessControl/RemoteControl/door/{doorNo}",
            "<RemoteControlDoor><cmd>open</cmd></RemoteControlDoor>", "application/xml", ct);

    public Task<HikResult> GetTimeAsync(HikDevice device, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Get, "/ISAPI/System/time", null, "application/xml", ct);

    public Task<HikResult> SetTimeAsync(HikDevice device, DateTime localTime, CancellationToken ct = default)
    {
        // Bakı: UTC+4. Hikvision timeZone POSIX işarəsi ilə: UTC+4 → "CST-4:00:00".
        var xml = $"<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                  $"<Time><timeMode>manual</timeMode>" +
                  $"<localTime>{Iso(localTime)}+04:00</localTime>" +
                  $"<timeZone>CST-4:00:00</timeZone></Time>";
        return SendAsync(device, HttpMethod.Put, "/ISAPI/System/time", xml, "application/xml", ct);
    }

    public async Task<HikResult> EnrollAccessNumberAsync(
        HikDevice device, string accessNumber, string name,
        DateTime beginTime, DateTime endTime, CancellationToken ct = default)
    {
        var user = new HikUser(accessNumber, name, beginTime, endTime);

        // İstifadəçini təmin et: əvvəl yaratmağa çalış, artıq varsa yenilə
        // (adı + etibarlılıq vaxtını düzəldir). Beləliklə enroll idempotentdir.
        var userResult = await CreateUserAsync(device, user, ct);
        if (!userResult.Success)
        {
            var updateResult = await UpdateUserAsync(device, user, ct);
            if (!updateResult.Success)
                return updateResult; // nə yaradıla, nə yenilənə bildi
            userResult = updateResult;
        }

        // Kartı bağla. Kart artıq bu nömrəyə bağlıdırsa uğur sayılır.
        var cardResult = await BindCardAsync(device, accessNumber, accessNumber, ct);
        if (!cardResult.Success && cardResult.SubStatusCode == "cardNoAlreadyExist")
            return userResult;

        return cardResult;
    }

    public async Task<HikResult> RevokeAccessNumberAsync(HikDevice device, string accessNumber, CancellationToken ct = default)
    {
        // Kartı əvvəl silmək daha təhlükəsizdir, sonra istifadəçini.
        await DeleteCardAsync(device, accessNumber, ct);
        return await DeleteUserAsync(device, accessNumber, ct);
    }

    public Task<HikResult> ConfigureEventHostAsync(HikDevice device, string serverIp, int serverPort,
        string path = "/api/hik/events", CancellationToken ct = default)
    {
        // Cihazın öz sxemasına uyğun (SubscribeEvent bloku tələb olunur — yoxsa badXmlContent).
        var xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<HttpHostNotification version=\"2.0\" xmlns=\"http://www.isapi.org/ver20/XMLSchema\">" +
            "<id>1</id>" +
            "<url>" + path + "</url>" +
            "<protocolType>HTTP</protocolType>" +
            "<parameterFormatType>JSON</parameterFormatType>" +
            "<addressingFormatType>ipaddress</addressingFormatType>" +
            "<ipAddress>" + serverIp + "</ipAddress>" +
            "<portNo>" + serverPort + "</portNo>" +
            "<httpAuthenticationMethod>none</httpAuthenticationMethod>" +
            "<SubscribeEvent>" +
              "<heartbeat>30</heartbeat>" +
              "<eventMode>all</eventMode>" +
              "<EventList>" +
                "<Event>" +
                  "<type>AccessControllerEvent</type>" +
                  "<pictureURLType>binary</pictureURLType>" +
                "</Event>" +
              "</EventList>" +
            "</SubscribeEvent>" +
            "</HttpHostNotification>";
        return SendAsync(device, HttpMethod.Put, "/ISAPI/Event/notification/httpHosts/1", xml, "application/xml", ct);
    }

    public Task<HikResult> GetEventHostAsync(HikDevice device, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Get, "/ISAPI/Event/notification/httpHosts", null, "application/xml", ct);

    public Task<HikResult> DeleteEventHostAsync(HikDevice device, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Delete, "/ISAPI/Event/notification/httpHosts/1", null, "application/xml", ct);

    public Task<HikResult> RebootAsync(HikDevice device, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Put, "/ISAPI/System/reboot", null, "application/xml", ct);

    public Task<HikResult> SearchAcsEventRawAsync(HikDevice device, DateTimeOffset start, DateTimeOffset end,
        int major, int minor, int position, CancellationToken ct = default)
    {
        var payload = new
        {
            AcsEventCond = new
            {
                searchID = Guid.NewGuid().ToString("N"),
                searchResultPosition = position,
                maxResults = 100,
                major,
                minor,
                startTime = IsoOffset(start),
                endTime = IsoOffset(end)
            }
        };
        return PostJson(device, "/ISAPI/AccessControl/AcsEvent?format=json", payload, ct);
    }

    public async Task<HikEventRawPage> SearchEventPageAsync(HikDevice device, DateTimeOffset start, DateTimeOffset end,
        int position, int maxResults, string? employeeNo = null, string? name = null, string? cardNo = null,
        int major = 0, int minor = 0, CancellationToken ct = default)
    {
        var page = new HikEventRawPage();
        var cond = new Dictionary<string, object>
        {
            ["searchID"] = Guid.NewGuid().ToString("N"),
            ["searchResultPosition"] = position,
            ["maxResults"] = maxResults,
            ["major"] = major,
            ["minor"] = minor,
            ["startTime"] = IsoOffset(start),
            ["endTime"] = IsoOffset(end),
            ["timeReverseOrder"] = true,           // ən yeni əvvəl
            ["picEnable"] = true                   // hadisə snapshot-larının pictureURL-ini qaytar
        };
        if (!string.IsNullOrWhiteSpace(employeeNo)) cond["employeeNoString"] = employeeNo!.Trim();
        if (!string.IsNullOrWhiteSpace(name)) cond["name"] = name!.Trim();
        if (!string.IsNullOrWhiteSpace(cardNo)) cond["cardNo"] = cardNo!.Trim();

        var res = await PostJson(device, "/ISAPI/AccessControl/AcsEvent?format=json",
            new { AcsEventCond = cond }, ct);
        if (!res.Success || string.IsNullOrWhiteSpace(res.RawBody))
        {
            page.Ok = false;
            page.Error = res.ErrorMessage ?? $"Cihaz cavab vermədi (HTTP {res.HttpStatus}).";
            return page;
        }

        try
        {
            using var doc = JsonDocument.Parse(res.RawBody);
            if (!doc.RootElement.TryGetProperty("AcsEvent", out var ae))
            {
                page.Ok = false;
                page.Error = "AcsEvent cavabı gözlənilməz formatdadır.";
                return page;
            }
            page.Ok = true;
            if (ae.TryGetProperty("responseStatusStrg", out var s)) page.Status = s.GetString();
            if (ae.TryGetProperty("totalMatches", out var tm) && tm.ValueKind == JsonValueKind.Number)
                page.Total = tm.GetInt32();

            if (ae.TryGetProperty("InfoList", out var infos) && infos.ValueKind == JsonValueKind.Array)
            {
                foreach (var e in infos.EnumerateArray())
                {
                    DateTimeOffset? t = null;
                    if (Str(e, "time") is { } ts && DateTimeOffset.TryParse(ts, out var parsed)) t = parsed;
                    page.Items.Add(new HikRawEvent
                    {
                        EmployeeNo = Str(e, "employeeNoString"),
                        Name = Str(e, "name"),
                        CardNo = Str(e, "cardNo"),
                        Major = Int(e, "major"),
                        Minor = Int(e, "minor"),
                        Time = t,
                        PictureUrl = Str(e, "pictureURL")
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            page.Ok = false;
            page.Error = "Cavab parse edilmədi: " + ex.Message;
        }
        return page;
    }

    public async Task<byte[]?> DownloadPictureAsync(HikDevice device, string pictureUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(pictureUrl)) return null;
        var client = GetClient(device);
        var url = pictureUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? pictureUrl
            : device.BaseUrl + (pictureUrl.StartsWith('/') ? pictureUrl : "/" + pictureUrl);
        try
        {
            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }

    public async Task<HikResult> UploadFaceAsync(HikDevice device, string personId, byte[] imageJpeg, CancellationToken ct = default)
    {
        var client = GetClient(device);
        var url = device.BaseUrl + "/ISAPI/Intelligent/FDLib/FaceDataRecord?format=json";
        try
        {
            var boundary = "----Hik" + Guid.NewGuid().ToString("N");
            using var content = new MultipartFormDataContent(boundary);
            var meta = JsonSerializer.Serialize(new { faceLibType = "blackFD", FDID = "1", FPID = personId });
            // Content-Type "application/json" (charset OLMADAN — Hikvision charset-i rədd edir).
            var metaPart = new StringContent(meta);
            metaPart.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            content.Add(metaPart, "FaceDataRecord");
            var img = new ByteArrayContent(imageJpeg);
            img.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(img, "img", "face.jpg");
            // .NET boundary-ni DIRNAQ içində qoyur; Hikvision parseri dırnağı qəbul etmir → dırnaqsız yenidən qur.
            content.Headers.Remove("Content-Type");
            content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");

            using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            using var resp = await client.SendAsync(req, ct);
            var raw = await resp.Content.ReadAsStringAsync(ct);
            return Parse((int)resp.StatusCode, resp.IsSuccessStatusCode, raw);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return HikResult.Fail($"Vaxt bitdi (cihaz {device.Ip}).");
        }
        catch (HttpRequestException ex)
        {
            return HikResult.Fail($"Bağlantı xətası: {ex.Message}");
        }
    }

    public Task<HikResult> GetFaceLibsAsync(HikDevice device, CancellationToken ct = default) =>
        SendAsync(device, HttpMethod.Get, "/ISAPI/Intelligent/FDLib?format=json", null, "application/json", ct);

    public async Task<DateTimeOffset?> GetDeviceTimeAsync(HikDevice device, CancellationToken ct = default)
    {
        var res = await GetTimeAsync(device, ct);
        if (!res.Success || string.IsNullOrWhiteSpace(res.RawBody)) return null;
        var m = System.Text.RegularExpressions.Regex.Match(res.RawBody, "<localTime>([^<]+)</localTime>");
        if (m.Success && DateTimeOffset.TryParse(m.Groups[1].Value, out var t)) return t;
        return null;
    }

    public async Task<List<HikEventDto>> SearchRecentEventsAsync(HikDevice device, DateTimeOffset start, DateTimeOffset end,
        CancellationToken ct = default)
    {
        var list = new List<HikEventDto>();
        var position = 0;

        // Bütün səhifələri çək (responseStatusStrg == "MORE" olduqca) — çox-işlək cihazda
        // təzə oxutmanın 30-luq limitin arxasında itməməsi üçün.
        for (var page = 0; page < 30; page++)
        {
            var res = await SearchAcsEventRawAsync(device, start, end, 0, 0, position, ct);
            if (!res.Success || string.IsNullOrWhiteSpace(res.RawBody))
                break;

            var returned = 0;
            string? status = null;
            try
            {
                using var doc = JsonDocument.Parse(res.RawBody);
                if (!doc.RootElement.TryGetProperty("AcsEvent", out var ae))
                    break;
                if (ae.TryGetProperty("responseStatusStrg", out var s))
                    status = s.GetString();
                if (ae.TryGetProperty("InfoList", out var infos) && infos.ValueKind == JsonValueKind.Array)
                {
                    foreach (var e in infos.EnumerateArray())
                    {
                        returned++;
                        var no = Str(e, "employeeNoString") ?? Str(e, "cardNo");
                        if (string.IsNullOrEmpty(no)) continue;
                        var minor = Int(e, "minor");
                        list.Add(new HikEventDto
                        {
                            AccessNumber = no,
                            PersonName = Str(e, "name"),
                            MajorType = Int(e, "major"),
                            MinorType = minor,
                            SerialNo = Int(e, "serialNo"),
                            DeviceIp = device.Ip,
                            // AcsEvent-də icazə verilmiş: kart/QR = minor 1, üz = minor 75.
                            Granted = minor is 1 or 75,
                            OccurredAt = DateTime.Now,
                            Raw = e.GetRawText()
                        });
                    }
                }
            }
            catch (JsonException) { break; }

            if (returned == 0 || status != "MORE")
                break;
            position += returned;
        }
        return list;
    }

    private static string IsoOffset(DateTimeOffset t) => t.ToString("yyyy-MM-ddTHH:mm:sszzz");
    private static string? Str(JsonElement e, string n) =>
        e.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
    private static int? Int(JsonElement e, string n) =>
        e.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.Number ? p.GetInt32() : null;
}
