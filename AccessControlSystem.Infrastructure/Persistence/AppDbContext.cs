using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    private readonly ICurrentTenant _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenant tenant) : base(options)
        => _tenant = tenant;

    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Host> Hosts => Set<Host>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Purpose> Purposes => Set<Purpose>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitArea> VisitAreas => Set<VisitArea>();
    public DbSet<VisitPurpose> VisitPurposes => Set<VisitPurpose>();
    public DbSet<Center> Centers => Set<Center>();
    public DbSet<Floor> Floors => Set<Floor>();
    public DbSet<AccessPoint> AccessPoints => Set<AccessPoint>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<VisitFloor> VisitFloors => Set<VisitFloor>();
    public DbSet<DeviceEnrollment> DeviceEnrollments => Set<DeviceEnrollment>();
    public DbSet<AccessEvent> AccessEvents => Set<AccessEvent>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<EmployeeFloor> EmployeeFloors => Set<EmployeeFloor>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // ---- Çoxkiracılı təcrid (global query filter) ----
        // CanSeeAllCompanies → qlobal admin, təhlükəsizlik məsulu, VƏ YA HTTP-siz sistem (hamısını görür).
        // Şirkət istifadəçisi → yalnız öz şirkətinin datası. (Yazma səlahiyyəti ayrıca IsGlobalAdmin-lə.)
        modelBuilder.Entity<Company>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.Id == _tenant.CompanyId);
        modelBuilder.Entity<AppUser>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Employee>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Department>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Position>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Center>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Floor>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<AccessPoint>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Device>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        // Qonaq domeni + audit loqları (şirkətə görə təcrid).
        modelBuilder.Entity<Host>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<Visit>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
        modelBuilder.Entity<SystemLog>().HasQueryFilter(e => _tenant.CanSeeAllCompanies || e.CompanyId == _tenant.CompanyId);
    }

    // IUnitOfWork.SaveChangesAsync — DbContext-in daxili implementasiyası ilə təmin olunur.
}
