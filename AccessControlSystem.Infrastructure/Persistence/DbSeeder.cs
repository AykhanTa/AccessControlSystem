using AccessControlSystem.Application.Interfaces.Services;
using AccessControlSystem.Domain.Entities;
using AccessControlSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AccessControlSystem.Infrastructure.Persistence;

/// <summary>Başlanğıc (seed) məlumatları — baza boş olduqda nümunə data əlavə edir.</summary>
public static class DbSeeder
{
    /// <summary>Bölmələr, rollar, icazələr və qorunan Global Administratoru bir dəfə yaradır.</summary>
    public static async Task SeedIdentityAsync(AppDbContext db, IPasswordHasher hasher)
    {
        if (await db.Roles.AnyAsync()) return;

        // --- Bölmələr (sidebar-a uyğun) ---
        var sectionDefs = new (string Code, string Name, int Sort)[]
        {
            ("dashboard", "Ana səhifə", 1),
            ("guests", "Qonaqlar", 2),
            ("cards", "Kartlar", 3),
            ("history", "Giriş-çıxış tarixçəsi", 4),
            ("active_permits", "Aktiv icazələr", 5),
            ("reports", "Hesabatlar", 6),
            ("logs", "Sistem Loqları", 7),
            ("users", "İstifadəçilər", 8),
            ("roles", "Rollar", 9),
            ("settings", "Parametrlər", 10),
        };
        var sections = sectionDefs
            .Select(x => new Section { Code = x.Code, Name = x.Name, SortOrder = x.Sort })
            .ToList();
        db.Sections.AddRange(sections);
        await db.SaveChangesAsync();

        Section S(string code) => sections.First(s => s.Code == code);
        RolePermission P(string code, bool v, bool a, bool e, bool d) =>
            new() { Section = S(code), CanView = v, CanAdd = a, CanEdit = e, CanDelete = d };

        // --- Administrator (qorunan sistem rolu) — bütün icazələr açıq ---
        var admin = new Role { Name = "Administrator", Description = "Sistem idarəetməsi", IsSystem = true };
        admin.Permissions = sections
            .Select(s => new RolePermission { Section = s, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true })
            .ToList();

        // --- Mühafizə ---
        var muhafize = new Role { Name = "Mühafizə", Description = "Giriş-çıxış nəzarəti", IsSystem = false };
        muhafize.Permissions = new List<RolePermission>
        {
            P("dashboard",      true,  true,  true,  false),
            P("guests",         false, false, true,  false),
            P("cards",          true,  true,  true,  false),
            P("history",        true,  true,  true,  false),
            P("active_permits", true,  true,  true,  false),
            P("reports",        false, false, false, false),
            P("logs",           true,  true,  true,  false),
            P("users",          false, false, false, false),
            P("roles",          false, false, false, false),
            P("settings",       false, false, false, false),
        };

        // --- Qəbul ---
        var qebul = new Role { Name = "Qəbul", Description = "Qonaq qəbulu", IsSystem = false };
        qebul.Permissions = new List<RolePermission>
        {
            P("dashboard",      true,  true,  false, false),
            P("guests",         true,  true,  true,  false),
            P("cards",          true,  false, false, false),
            P("history",        true,  false, false, false),
            P("active_permits", true,  true,  true,  false),
            P("reports",        false, false, false, false),
            P("logs",           false, false, false, false),
            P("users",          false, false, false, false),
            P("roles",          false, false, false, false),
            P("settings",       false, false, false, false),
        };

        db.Roles.AddRange(admin, muhafize, qebul);
        await db.SaveChangesAsync();

        // --- Qorunan Global Administrator (bir dəfə yaradılır) ---
        db.Users.Add(new AppUser
        {
            FirstName = "Ayxan",
            LastName = "Tağızadə",
            Email = "admin@admin.az",
            PasswordHash = hasher.Hash("Admin123!"),
            RoleId = admin.Id,
            Status = UserStatus.Active,
            IsProtected = true
        });
        await db.SaveChangesAsync();
    }

    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Hosts.AnyAsync()) return; // artıq seed edilib

        // --- Qəbul edən şəxs ---
        var host = new Host { FirstName = "İlkin", LastName = "Nəsirli", Email = "ilkin.nesirli@company.com", Department = "İdarəetmə", IsActive = true };
        db.Hosts.Add(host);

        // --- Ərazilər ---
        var a1 = new Area { Name = "1-ci mərtəbə, İclas otağı" };
        var a2 = new Area { Name = "B korpusu – Qəbul sahəsi" };
        var a3 = new Area { Name = "Baş Ofis – Əsas Giriş" };
        db.Areas.AddRange(a1, a2, a3);

        // --- Məqsədlər ---
        var pAudit = new Purpose { Name = "Audit" };
        var pDiger = new Purpose { Name = "Digər" };
        var pGrc = new Purpose { Name = "GRC" };
        var pIclas = new Purpose { Name = "İclas" };
        var pGorus = new Purpose { Name = "İş görüşməsi" };
        var pSened = new Purpose { Name = "Sənəd təslimi" };
        db.Purposes.AddRange(pAudit, pDiger, pGrc, pIclas, pGorus, pSened);

        // --- Kartlar ---
        var c23 = new Card { CardNo = "MK-24-00023", Note = "Ehtiyyat kart", Status = CardStatus.Free, IsActive = true };
        var c87 = new Card { CardNo = "MK-24-00087", Note = "Yeni kart", Status = CardStatus.Assigned, IsActive = true };
        var c76 = new Card { CardNo = "MK-34-00076", Note = "", Status = CardStatus.Free, IsActive = true };
        var c94 = new Card { CardNo = "MK-45-00094", Note = "Kart Zədəlidir Dəyişilməlidir", Status = CardStatus.Assigned, IsActive = true };
        db.Cards.AddRange(c23, c87, c76, c94);

        // --- Qonaqlar + Ziyarətlər ---
        var gTural = new Guest { FirstName = "Tural", LastName = "Hüseynzadə", IdDocument = "AZE1234567", PhotoPath = "img/tural.jpg" };
        var gElsen = new Guest { FirstName = "Elşən", LastName = "Musayev", IdDocument = "AA232323232", PhotoPath = "img/elsen.jpg" };
        var gAysel = new Guest { FirstName = "Aysel", LastName = "Həsənli", IdDocument = "AA3128789", PhotoPath = "img/aysel.jpg" };
        var gMehemmed = new Guest { FirstName = "Mehemmed", LastName = "Sultanli", IdDocument = "AA2233445566", PhotoPath = "img/mehemmed.jpg" };
        var gSenan = new Guest { FirstName = "Senan", LastName = "Abbasov", IdDocument = "AA1122334455", PhotoPath = "img/senan.jpg" };
        db.Guests.AddRange(gTural, gElsen, gAysel, gMehemmed, gSenan);

        // Tural — kartla, çıxıb
        var v1 = new Visit
        {
            Guest = gTural, Host = host, PassType = PassType.Card, Card = c23,
            ArrivalAt = new DateTime(2026, 7, 9, 16, 0, 0), ExpectedExitAt = new DateTime(2026, 7, 9, 18, 0, 0),
            ActualExitAt = new DateTime(2026, 7, 9, 14, 46, 0), Status = VisitStatus.Out,
            CreatedAt = new DateTime(2026, 7, 9, 15, 50, 0)
        };
        v1.VisitAreas.Add(new VisitArea { Area = a3 });
        v1.VisitPurposes.Add(new VisitPurpose { Purpose = pAudit });
        v1.VisitPurposes.Add(new VisitPurpose { Purpose = pGorus });

        // Elşən — QR, çıxıb
        var v2 = new Visit
        {
            Guest = gElsen, Host = host, PassType = PassType.Qr, QrToken = Guid.NewGuid().ToString("N"),
            ArrivalAt = new DateTime(2026, 7, 9, 18, 0, 0), ExpectedExitAt = new DateTime(2026, 7, 9, 20, 0, 0),
            ActualExitAt = new DateTime(2026, 7, 9, 14, 37, 0), Status = VisitStatus.Out,
            CreatedAt = new DateTime(2026, 7, 9, 17, 55, 0)
        };
        v2.VisitAreas.Add(new VisitArea { Area = a1 });
        v2.VisitPurposes.Add(new VisitPurpose { Purpose = pIclas });
        v2.VisitPurposes.Add(new VisitPurpose { Purpose = pSened });

        // Aysel — kartla, binadadır
        var v3 = new Visit
        {
            Guest = gAysel, Host = host, PassType = PassType.Card, Card = c94,
            ArrivalAt = new DateTime(2026, 7, 13, 12, 0, 0), ExpectedExitAt = new DateTime(2026, 7, 15, 18, 0, 0),
            Status = VisitStatus.In, CreatedAt = new DateTime(2026, 7, 13, 11, 50, 0)
        };
        v3.VisitAreas.Add(new VisitArea { Area = a1 });
        v3.VisitAreas.Add(new VisitArea { Area = a2 });
        v3.VisitPurposes.Add(new VisitPurpose { Purpose = pAudit });
        v3.VisitPurposes.Add(new VisitPurpose { Purpose = pDiger });
        v3.VisitPurposes.Add(new VisitPurpose { Purpose = pGrc });
        v3.VisitPurposes.Add(new VisitPurpose { Purpose = pIclas });

        // Mehemmed — kartla, gecikib
        var v4 = new Visit
        {
            Guest = gMehemmed, Host = host, PassType = PassType.Card, Card = c87,
            ArrivalAt = new DateTime(2026, 7, 11, 10, 0, 0), ExpectedExitAt = new DateTime(2026, 7, 11, 17, 0, 0),
            Status = VisitStatus.Late, CreatedAt = new DateTime(2026, 7, 11, 9, 50, 0)
        };
        v4.VisitAreas.Add(new VisitArea { Area = a1 });
        v4.VisitPurposes.Add(new VisitPurpose { Purpose = pGorus });

        // Senan — QR, çıxıb
        var v5 = new Visit
        {
            Guest = gSenan, Host = host, PassType = PassType.Qr, QrToken = Guid.NewGuid().ToString("N"),
            ArrivalAt = new DateTime(2026, 7, 8, 10, 0, 0), ExpectedExitAt = new DateTime(2026, 7, 8, 12, 0, 0),
            ActualExitAt = new DateTime(2026, 7, 8, 10, 39, 0), Status = VisitStatus.Out,
            CreatedAt = new DateTime(2026, 7, 8, 9, 55, 0)
        };
        v5.VisitAreas.Add(new VisitArea { Area = a2 });
        v5.VisitPurposes.Add(new VisitPurpose { Purpose = pGorus });

        db.Visits.AddRange(v1, v2, v3, v4, v5);

        await db.SaveChangesAsync();
    }
}
