using AccessControlSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AccessControlSystem.Infrastructure.Persistence.Configurations;

public class GuestConfiguration : IEntityTypeConfiguration<Guest>
{
    public void Configure(EntityTypeBuilder<Guest> b)
    {
        b.ToTable("Guests");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        b.Property(x => x.Patronymic).HasMaxLength(80);
        b.Property(x => x.IdDocument).HasMaxLength(40).IsRequired();
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(160);
        b.Property(x => x.PhotoPath).HasMaxLength(255);
        b.Property(x => x.DocumentPath).HasMaxLength(255);
        b.Ignore(x => x.FullName);
        b.HasIndex(x => x.IdDocument).IsUnique();
        b.HasIndex(x => new { x.LastName, x.FirstName });
    }
}

public class HostConfiguration : IEntityTypeConfiguration<Host>
{
    public void Configure(EntityTypeBuilder<Host> b)
    {
        b.ToTable("Hosts");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        b.Property(x => x.Email).HasMaxLength(160);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Department).HasMaxLength(120);
        b.Ignore(x => x.FullName);
    }
}

public class AreaConfiguration : IEntityTypeConfiguration<Area>
{
    public void Configure(EntityTypeBuilder<Area> b)
    {
        b.ToTable("Areas");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class PurposeConfiguration : IEntityTypeConfiguration<Purpose>
{
    public void Configure(EntityTypeBuilder<Purpose> b)
    {
        b.ToTable("Purposes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> b)
    {
        b.ToTable("Cards");
        b.HasKey(x => x.Id);
        b.Property(x => x.CardNo).HasMaxLength(40).IsRequired();
        b.Property(x => x.Note).HasMaxLength(255);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.CardNo).IsUnique();
    }
}

public class VisitConfiguration : IEntityTypeConfiguration<Visit>
{
    public void Configure(EntityTypeBuilder<Visit> b)
    {
        b.ToTable("Visits");
        b.HasKey(x => x.Id);
        b.Property(x => x.PassType).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.QrToken).HasMaxLength(64);
        b.Property(x => x.Note).HasMaxLength(500);

        b.HasOne(x => x.Guest).WithMany(g => g.Visits)
            .HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Host).WithMany(h => h.Visits)
            .HasForeignKey(x => x.HostId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Card).WithMany(c => c.Visits)
            .HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);

        b.HasIndex(x => x.QrToken).IsUnique().HasFilter("[QrToken] IS NOT NULL");
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ArrivalAt);
    }
}

public class VisitAreaConfiguration : IEntityTypeConfiguration<VisitArea>
{
    public void Configure(EntityTypeBuilder<VisitArea> b)
    {
        b.ToTable("VisitAreas");
        b.HasKey(x => new { x.VisitId, x.AreaId });
        b.HasOne(x => x.Visit).WithMany(v => v.VisitAreas)
            .HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Area).WithMany(a => a.VisitAreas)
            .HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class VisitPurposeConfiguration : IEntityTypeConfiguration<VisitPurpose>
{
    public void Configure(EntityTypeBuilder<VisitPurpose> b)
    {
        b.ToTable("VisitPurposes");
        b.HasKey(x => new { x.VisitId, x.PurposeId });
        b.HasOne(x => x.Visit).WithMany(v => v.VisitPurposes)
            .HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Purpose).WithMany(p => p.VisitPurposes)
            .HasForeignKey(x => x.PurposeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SectionConfiguration : IEntityTypeConfiguration<Section>
{
    public void Configure(EntityTypeBuilder<Section> b)
    {
        b.ToTable("Sections");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("Roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(80).IsRequired();
        b.Property(x => x.Description).HasMaxLength(255);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("RolePermissions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.RoleId, x.SectionId }).IsUnique();
        b.HasOne(x => x.Role).WithMany(r => r.Permissions)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Section).WithMany(s => s.RolePermissions)
            .HasForeignKey(x => x.SectionId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> b)
    {
        b.ToTable("SystemLogs");
        b.HasKey(x => x.Id);
        b.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        b.Property(x => x.Action).HasMaxLength(80).IsRequired();
        b.Property(x => x.EntityType).HasMaxLength(60);
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(45);
        b.HasIndex(x => x.CreatedAt);
        b.HasIndex(x => x.Action);
    }
}

public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("Users");
        b.HasKey(x => x.Id);
        b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        b.Property(x => x.Email).HasMaxLength(160).IsRequired();
        b.Property(x => x.PasswordHash).HasMaxLength(255).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.FullName);
        b.HasIndex(x => x.Email).IsUnique();
        b.HasOne(x => x.Role).WithMany(r => r.Users)
            .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}
