namespace AccessControlSystem.Application.DTOs;

/// <summary>Ana səhifə statistik kartları.</summary>
public class DashboardStatsDto
{
    public int TodayRegistered { get; set; }   // Bu gün qeydə alınan
    public int CurrentlyIn { get; set; }        // Hazırda binada
    public int TodayExited { get; set; }        // Bu gün çıxış edən
    public int LateExits { get; set; }          // Gecikmiş çıxış
    public int FreeCards { get; set; }          // İstifadəyə hazır kart
    public int ActiveSessions { get; set; }     // Aktiv sistem sessiyası
}
