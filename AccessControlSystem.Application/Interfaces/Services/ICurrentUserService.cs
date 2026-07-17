namespace AccessControlSystem.Application.Interfaces.Services;

/// <summary>Cari sorğunu icra edən istifadəçi haqqında məlumat (Web qatında implementasiya olunur).</summary>
public interface ICurrentUserService
{
    long? UserId { get; }
    string UserName { get; }     // autentifikasiya yoxdursa "Sistem"
    string? IpAddress { get; }
}
