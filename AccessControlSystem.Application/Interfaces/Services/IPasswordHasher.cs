namespace AccessControlSystem.Application.Interfaces.Services;

/// <summary>Şifrələrin təhlükəsiz hash-lanması (açıq şifrə saxlanılmır).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
