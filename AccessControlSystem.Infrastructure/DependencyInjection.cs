using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Infrastructure.Persistence;
using AccessControlSystem.Infrastructure.Repositories;
using AccessControlSystem.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AccessControlSystem.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Infrastructure qatı: DbContext (MSSQL) və repository-lər.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Unit of Work — DbContext-in özü
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        // Repository-lər
        services.AddScoped<IVisitRepository, VisitRepository>();
        services.AddScoped<ICardRepository, CardRepository>();
        services.AddScoped<IGuestRepository, GuestRepository>();
        services.AddScoped<IHostRepository, HostRepository>();
        services.AddScoped<IAreaRepository, AreaRepository>();
        services.AddScoped<IPurposeRepository, PurposeRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ISystemLogRepository, SystemLogRepository>();

        // Təhlükəsizlik
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
