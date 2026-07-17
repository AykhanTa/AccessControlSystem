using AccessControlSystem.Application.Interfaces.Repositories;
using AccessControlSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Host> Hosts => Set<Host>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Purpose> Purposes => Set<Purpose>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<VisitArea> VisitAreas => Set<VisitArea>();
    public DbSet<VisitPurpose> VisitPurposes => Set<VisitPurpose>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<SystemLog> SystemLogs => Set<SystemLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    // IUnitOfWork.SaveChangesAsync — DbContext-in daxili implementasiyası ilə təmin olunur.
}
