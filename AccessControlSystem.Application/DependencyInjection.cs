using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AccessControlSystem.Application;

public static class DependencyInjection
{
    /// <summary>Application qatının servislərini qeydiyyata alır.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IGuestService, GuestService>();
        services.AddScoped<ICardService, CardService>();
        services.AddScoped<IActivePermitService, ActivePermitService>();
        services.AddScoped<IHistoryService, HistoryService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISystemLogWriter, SystemLogWriter>();
        services.AddScoped<ISystemLogService, SystemLogService>();
        services.AddScoped<ILookupService, LookupService>();
        return services;
    }
}
