using AccessControlSystem.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AccessControlSystem.Infrastructure.Persistence;

/// <summary>
/// Design-time (dotnet ef migrations) üçün DbContext yaradıcısı.
/// Tətbiqi işə salmadan miqrasiya yaratmağa imkan verir.
/// AppDbContext ICurrentTenant tələb etdiyindən qlobal (filtrsiz) stub veririk.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        const string connectionString =
            "Server=localhost\\SQLEXPRESS01;Database=AccessControlDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options, new GlobalTenant());
    }

    private sealed class GlobalTenant : ICurrentTenant
    {
        public bool IsGlobalAdmin => true;
        public bool CanSeeAllCompanies => true;
        public long? CompanyId => null;
    }
}
