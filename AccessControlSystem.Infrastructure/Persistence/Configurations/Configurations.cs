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
        b.Property(x => x.AccessNumber).HasMaxLength(20);
        b.Property(x => x.Note).HasMaxLength(500);

        b.HasOne(x => x.Guest).WithMany(g => g.Visits)
            .HasForeignKey(x => x.GuestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Host).WithMany(h => h.Visits)
            .HasForeignKey(x => x.HostId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Card).WithMany(c => c.Visits)
            .HasForeignKey(x => x.CardId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.NoAction);

        b.HasIndex(x => x.QrToken).IsUnique().HasFilter("[QrToken] IS NOT NULL");
        b.HasIndex(x => x.AccessNumber).HasFilter("[AccessNumber] IS NOT NULL");
        b.HasIndex(x => x.Status);
        b.HasIndex(x => x.ArrivalAt);
    }
}

public class CenterConfiguration : IEntityTypeConfiguration<Center>
{
    public void Configure(EntityTypeBuilder<Center> b)
    {
        b.ToTable("Centers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.Address).HasMaxLength(255);
        b.Property(x => x.City).HasMaxLength(120);
        b.Property(x => x.TimeZone).HasMaxLength(40);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class FloorConfiguration : IEntityTypeConfiguration<Floor>
{
    public void Configure(EntityTypeBuilder<Floor> b)
    {
        b.ToTable("Floors");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.HasIndex(x => x.Name).IsUnique();
        b.HasOne(x => x.Center).WithMany(c => c.Floors)
            .HasForeignKey(x => x.CenterId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class AccessPointConfiguration : IEntityTypeConfiguration<AccessPoint>
{
    public void Configure(EntityTypeBuilder<AccessPoint> b)
    {
        b.ToTable("AccessPoints");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.PointType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10);
        // Restrict — Floor→Device onsuz da cascade-dir; çoxlu cascade yolunun qarşısını alır.
        b.HasOne(x => x.Floor).WithMany()
            .HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Center).WithMany()
            .HasForeignKey(x => x.CenterId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> b)
    {
        b.ToTable("Devices");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Ip).HasMaxLength(64).IsRequired();
        b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.SerialNo).HasMaxLength(80);
        b.Property(x => x.Model).HasMaxLength(120);
        b.HasOne(x => x.Floor).WithMany(f => f.Devices)
            .HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AccessPoint).WithMany(a => a.Devices)
            .HasForeignKey(x => x.AccessPointId).OnDelete(DeleteBehavior.SetNull);
        // Bir mərtəbədə eyni istiqamətdə eyni IP təkrarlanmasın.
        b.HasIndex(x => new { x.Ip, x.Port }).IsUnique();
    }
}

public class VisitFloorConfiguration : IEntityTypeConfiguration<VisitFloor>
{
    public void Configure(EntityTypeBuilder<VisitFloor> b)
    {
        b.ToTable("VisitFloors");
        b.HasKey(x => new { x.VisitId, x.FloorId });
        b.HasOne(x => x.Visit).WithMany(v => v.VisitFloors)
            .HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Floor).WithMany(f => f.VisitFloors)
            .HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> b)
    {
        b.ToTable("Companies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.TaxNumber).HasMaxLength(40);
        b.Property(x => x.ContactPerson).HasMaxLength(160);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(160);
        b.HasIndex(x => x.Name);
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("Departments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.HasOne(x => x.Company).WithMany(c => c.Departments)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ParentDepartment).WithMany(d => d.SubDepartments)
            .HasForeignKey(x => x.ParentDepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.WorkSchedule).WithMany()
            .HasForeignKey(x => x.WorkScheduleId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class WorkScheduleConfiguration : IEntityTypeConfiguration<WorkSchedule>
{
    public void Configure(EntityTypeBuilder<WorkSchedule> b)
    {
        b.ToTable("WorkSchedules");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Color).HasMaxLength(20);
        b.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CompanyId);
        // OwnerEmployeeId — yumşaq istinad (FK constraint yox, cascade dövrünün qarşısını alır).
        b.HasIndex(x => x.OwnerEmployeeId);
    }
}

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> b)
    {
        b.ToTable("Positions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.HasOne(x => x.Company).WithMany(c => c.Positions)
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");
        b.HasKey(x => x.Id);
        b.Property(x => x.EmployeeNo).HasMaxLength(40).IsRequired();
        b.Property(x => x.FirstName).HasMaxLength(80).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(80).IsRequired();
        b.Property(x => x.Patronymic).HasMaxLength(80);
        b.Property(x => x.FinCode).HasMaxLength(20);
        b.Property(x => x.DocumentNo).HasMaxLength(40);
        b.Property(x => x.Phone).HasMaxLength(40);
        b.Property(x => x.Email).HasMaxLength(160);
        b.Property(x => x.PhotoPath).HasMaxLength(255);
        b.Property(x => x.AccessNumber).HasMaxLength(20);
        b.Property(x => x.DeviceNumbers).HasMaxLength(200);
        b.Property(x => x.DeviceName).HasMaxLength(120);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.FaceStatus).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.CurrentPresence).HasConversion<string>().HasMaxLength(10);
        b.Ignore(x => x.FullName);
        b.HasIndex(x => x.EmployeeNo).IsUnique();
        b.HasIndex(x => x.AccessNumber).HasFilter("[AccessNumber] IS NOT NULL");
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.WorkSchedule).WithMany().HasForeignKey(x => x.WorkScheduleId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class LeaveTypeConfiguration : IEntityTypeConfiguration<LeaveType>
{
    public void Configure(EntityTypeBuilder<LeaveType> b)
    {
        b.ToTable("LeaveTypes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.Color).HasMaxLength(20);
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CompanyId);
    }
}

public class LeaveRecordConfiguration : IEntityTypeConfiguration<LeaveRecord>
{
    public void Configure(EntityTypeBuilder<LeaveRecord> b)
    {
        b.ToTable("LeaveRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).HasMaxLength(400);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.LeaveType).WithMany().HasForeignKey(x => x.LeaveTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.EmployeeId, x.StartDate, x.EndDate });
        b.HasIndex(x => x.CompanyId);
    }
}

public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> b)
    {
        b.ToTable("Holidays");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.HasOne(x => x.Company).WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.CompanyId, x.Date });
    }
}

public class EmployeeFloorConfiguration : IEntityTypeConfiguration<EmployeeFloor>
{
    public void Configure(EntityTypeBuilder<EmployeeFloor> b)
    {
        b.ToTable("EmployeeFloors");
        b.HasKey(x => new { x.EmployeeId, x.FloorId });
        b.HasOne(x => x.Employee).WithMany(e => e.EmployeeFloors)
            .HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Floor).WithMany()
            .HasForeignKey(x => x.FloorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class AccessEventConfiguration : IEntityTypeConfiguration<AccessEvent>
{
    public void Configure(EntityTypeBuilder<AccessEvent> b)
    {
        b.ToTable("AccessEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.AccessNumber).HasMaxLength(20);
        b.Property(x => x.PersonName).HasMaxLength(160);
        b.Property(x => x.EventType).HasMaxLength(60);
        b.Property(x => x.DeviceIp).HasMaxLength(64);
        b.Property(x => x.Raw).HasMaxLength(4000);
        b.HasOne(x => x.Visit).WithMany().HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Device).WithMany().HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.SetNull);
        b.HasIndex(x => x.OccurredAt);
        b.HasIndex(x => x.AccessNumber);
    }
}

public class DeviceEnrollmentConfiguration : IEntityTypeConfiguration<DeviceEnrollment>
{
    public void Configure(EntityTypeBuilder<DeviceEnrollment> b)
    {
        b.ToTable("DeviceEnrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.AccessNumber).HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.LastError).HasMaxLength(500);
        b.HasOne(x => x.Visit).WithMany(v => v.DeviceEnrollments)
            .HasForeignKey(x => x.VisitId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Device).WithMany(d => d.Enrollments)
            .HasForeignKey(x => x.DeviceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.VisitId, x.DeviceId }).IsUnique();
        b.HasIndex(x => x.Status);
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
        b.HasOne(x => x.Company).WithMany()
            .HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}
