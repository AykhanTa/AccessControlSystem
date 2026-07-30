using AccessControlSystem.Application.DTOs;

namespace AccessControlSystem.Application.Interfaces.Services;

/// <summary>
/// Bir Hikvision cihazı ilə ISAPI (JSON, Digest auth) üzərindən danışan servis.
/// Bütün əməliyyatlar bir konkret cihaza (<see cref="HikDevice"/>) qarşı işləyir —
/// çoxcihazlı yazma/silmə məntiqi bunun üzərində qurulacaq (Faza 3).
/// </summary>
public interface IHikvisionDeviceService
{
    /// <summary>Bağlantı və kredit yoxlaması (GET /ISAPI/System/deviceInfo).</summary>
    Task<HikResult> TestConnectionAsync(HikDevice device, CancellationToken ct = default);

    /// <summary>İstifadəçi yaradır (POST UserInfo/Record).</summary>
    Task<HikResult> CreateUserAsync(HikDevice device, HikUser user, CancellationToken ct = default);

    /// <summary>İstifadəçini yeniləyir (PUT UserInfo/Modify).</summary>
    Task<HikResult> UpdateUserAsync(HikDevice device, HikUser user, CancellationToken ct = default);

    /// <summary>Kartı istifadəçiyə bağlayır (POST CardInfo/Record).</summary>
    Task<HikResult> BindCardAsync(HikDevice device, string employeeNo, string cardNo, CancellationToken ct = default);

    /// <summary>İstifadəçini silir (PUT UserInfo/Delete).</summary>
    Task<HikResult> DeleteUserAsync(HikDevice device, string employeeNo, CancellationToken ct = default);

    /// <summary>Kartı silir (PUT CardInfo/Delete).</summary>
    Task<HikResult> DeleteCardAsync(HikDevice device, string cardNo, CancellationToken ct = default);

    /// <summary>İstifadəçini axtarır (POST UserInfo/Search).</summary>
    Task<HikResult> SearchUserAsync(HikDevice device, string employeeNo, CancellationToken ct = default);

    /// <summary>Qapını uzaqdan açır (PUT RemoteControl/door/{doorNo}).</summary>
    Task<HikResult> RemoteOpenDoorAsync(HikDevice device, int doorNo = 1, CancellationToken ct = default);

    /// <summary>Cihazın saatını oxuyur (GET /ISAPI/System/time).</summary>
    Task<HikResult> GetTimeAsync(HikDevice device, CancellationToken ct = default);

    /// <summary>Cihazın saatını təyin edir (PUT /ISAPI/System/time).</summary>
    Task<HikResult> SetTimeAsync(HikDevice device, DateTime localTime, CancellationToken ct = default);

    /// <summary>
    /// Bir addımda tam qeydiyyat: istifadəçi yarat (Valid = ziyarət müddəti,
    /// 24/7 icazə) + eyni nömrəni kart kimi bağla. AccessNumber = employeeNo = cardNo.
    /// </summary>
    Task<HikResult> EnrollAccessNumberAsync(
        HikDevice device, string accessNumber, string name,
        DateTime beginTime, DateTime endTime, CancellationToken ct = default);

    /// <summary>Bir addımda tam təmizləmə: kartı sil + istifadəçini sil.</summary>
    Task<HikResult> RevokeAccessNumberAsync(HikDevice device, string accessNumber, CancellationToken ct = default);

    /// <summary>
    /// Cihazı real-vaxt hadisələri serverə göndərməyə konfiqurasiya edir
    /// (PUT /ISAPI/Event/notification/httpHosts/1). Server ünvanı: {serverIp}:{serverPort}{path}.
    /// </summary>
    Task<HikResult> ConfigureEventHostAsync(HikDevice device, string serverIp, int serverPort,
        string path = "/api/hik/events", CancellationToken ct = default);

    /// <summary>Cihazdakı hazırkı httpHosts konfiqurasiyasını oxuyur (diaqnostika).</summary>
    Task<HikResult> GetEventHostAsync(HikDevice device, CancellationToken ct = default);

    /// <summary>httpHosts (id=1) konfiqini silir — gözləyən event yığınını sıfırlamaq üçün.</summary>
    Task<HikResult> DeleteEventHostAsync(HikDevice device, CancellationToken ct = default);

    /// <summary>Cihazı yenidən başladır (PUT /ISAPI/System/reboot) — gözləyən event növbəsini təmizləmək üçün.</summary>
    Task<HikResult> RebootAsync(HikDevice device, CancellationToken ct = default);

    /// <summary>
    /// Cihazın hadisə jurnalını vaxt aralığı ilə sorğular (POST AcsEvent, yalnız icazə verilənlər).
    /// httpHosts backlog-unu keçmək üçün PULL yanaşması — yalnız təzə (verilmiş aralıqdakı) hadisələr.
    /// </summary>
    Task<List<HikEventDto>> SearchRecentEventsAsync(HikDevice device, DateTimeOffset start, DateTimeOffset end,
        CancellationToken ct = default);

    /// <summary>AcsEvent axtarışının XAM cavabını qaytarır (diaqnostika — sorğu formatını yoxlamaq).</summary>
    Task<HikResult> SearchAcsEventRawAsync(HikDevice device, DateTimeOffset start, DateTimeOffset end,
        int major, int minor, int position, CancellationToken ct = default);

    /// <summary>
    /// Cihaz jurnalının BİR səhifəsini (bütün hadisə tipləri: qapı, login, üz və s.) + ümumi sayı qaytarır.
    /// Server tərəfli pagination üçün: position = offset, maxResults = səhifə ölçüsü. Ən yenidən köhnəyə.
    /// employeeNo/name/cardNo verilibsə cihaz özü süzür (totalMatches süzülmüş sayı göstərir).
    /// </summary>
    Task<HikEventRawPage> SearchEventPageAsync(HikDevice device, DateTimeOffset start, DateTimeOffset end,
        int position, int maxResults, string? employeeNo = null, string? name = null, string? cardNo = null,
        int major = 0, int minor = 0, CancellationToken ct = default);

    /// <summary>Cihazdan bir şəkli (hadisə snapshot-u) Digest auth ilə yükləyir. Uğursuzsa null.</summary>
    Task<byte[]?> DownloadPictureAsync(HikDevice device, string pictureUrl, CancellationToken ct = default);

    /// <summary>Cihazın hazırkı yerli vaxtını (öz timezone-u ilə) qaytarır — vaxt-pəncərəli sorğular üçün.</summary>
    Task<DateTimeOffset?> GetDeviceTimeAsync(HikDevice device, CancellationToken ct = default);

    /// <summary>
    /// İşçinin üzünü cihaza yükləyir (POST /ISAPI/Intelligent/FDLib/FaceDataRecord, multipart).
    /// personId = cihazdakı employeeNo (FPID). Şəkil JPEG.
    /// </summary>
    Task<HikResult> UploadFaceAsync(HikDevice device, string personId, byte[] imageJpeg, CancellationToken ct = default);

    /// <summary>Cihazdakı üz kitabxanalarını oxuyur (GET /ISAPI/Intelligent/FDLib) — diaqnostika.</summary>
    Task<HikResult> GetFaceLibsAsync(HikDevice device, CancellationToken ct = default);
}
