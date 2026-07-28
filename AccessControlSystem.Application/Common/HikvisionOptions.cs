namespace AccessControlSystem.Application.Common;

/// <summary>
/// Bütün Hikvision cihazları üçün paylaşılan admin krediti (konfiqurasiyadan).
/// Cihazın IP/Port/UseHttps dəyərləri Device entity-dən, login/parol isə buradan gəlir.
/// </summary>
public class HikvisionOptions
{
    public string Username { get; set; } = "admin";
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 80;
    public bool UseHttps { get; set; } = false;
}
