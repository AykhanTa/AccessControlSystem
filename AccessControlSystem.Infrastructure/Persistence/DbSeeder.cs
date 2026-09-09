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

    /// <summary>
    /// Mərtəbə + cihaz başlanğıc datası. Cihazlar cədvəli boş olduqda işləyir —
    /// yəni mövcud (artıq seed edilmiş) bazaya da bir dəfə tətbiq olunur.
    /// Həmçinin real test kartını (Mifare UID) təmin edir.
    /// </summary>
    /// <summary>
    /// Default mərkəz (bina) + mövcud mərtəbələri ona bağlayır. Çoxbina təməli.
    /// Mərkəzlər boş olduqda işləyir — mövcud bazaya da bir dəfə tətbiq olunur.
    /// </summary>
    public static async Task SeedCentersAsync(AppDbContext db)
    {
        Center center;
        if (!await db.Centers.AnyAsync())
        {
            center = new Center
            {
                Code = "HQ",
                Name = "Baş Ofis",
                City = "Bakı",
                TimeZone = "CST-4:00:00",
                IsActive = true
            };
            db.Centers.Add(center);
            await db.SaveChangesAsync();
        }
        else
        {
            center = await db.Centers.OrderBy(c => c.Id).FirstAsync();
        }

        // Mərkəzsiz mərtəbələri default mərkəzə bağla.
        var orphanFloors = await db.Floors.Where(f => f.CenterId == null).ToListAsync();
        if (orphanFloors.Count > 0)
        {
            foreach (var f in orphanFloors) f.CenterId = center.Id;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>Default şirkət — işçilər/şöbələr/vəzifələr üçün. Şirkətlər boş olduqda işləyir.</summary>
    public static async Task SeedOrganizationAsync(AppDbContext db)
    {
        if (await db.Companies.AnyAsync()) return;
        db.Companies.Add(new Company { Name = "Baş Şirkət", IsActive = true });
        await db.SaveChangesAsync();
    }

    /// <summary>Default məzuniyyət/ezamiyyət növlərini (qlobal) bir dəfə əlavə edir.</summary>
    public static async Task SeedLeaveTypesAsync(AppDbContext db)
    {
        if (await db.LeaveTypes.IgnoreQueryFilters().AnyAsync()) return;
        db.LeaveTypes.AddRange(
            new LeaveType { Name = "İllik məzuniyyət", CountsAsWorked = false, Paid = true, Color = "#8b5cf6", IsActive = true },
            new LeaveType { Name = "Xəstəlik (bülleten)", CountsAsWorked = false, Paid = true, Color = "#06b6d4", IsActive = true },
            new LeaveType { Name = "Ödənişsiz məzuniyyət", CountsAsWorked = false, Paid = false, Color = "#94a3b8", IsActive = true },
            new LeaveType { Name = "Ezamiyyət", CountsAsWorked = true, Paid = true, Color = "#0ea5e9", IsActive = true });
        await db.SaveChangesAsync();
    }

    /// <summary>"İşçilər" bölməsini (Section) və Administrator roluna icazəsini bir dəfə əlavə edir.</summary>
    public static async Task SeedEmployeesSectionAsync(AppDbContext db)
    {
        if (await db.Sections.AnyAsync(s => s.Code == "employees")) return;
        var section = new Section { Code = "employees", Name = "İşçilər", SortOrder = 11 };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole is not null)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id, SectionId = section.Id,
                CanView = true, CanAdd = true, CanEdit = true, CanDelete = true
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// DeviceName boş olan işçilər üçün onu keçmiş keçid hadisələrindəki (AccessEvents.PersonName)
    /// AYDIN DOMİNANT addan avtomatik doldurur (≥75%). Cihazdakı ad əl ilə yazılmadan öyrənilir;
    /// qarışıq/az data olanlar admin üçün boş qalır.
    /// </summary>
    public static async Task SeedEmployeeDeviceNamesAsync(AppDbContext db)
    {
        var employees = await db.Employees.Where(e => e.DeviceName == null).ToListAsync();
        if (employees.Count == 0) return;
        var empIds = employees.Select(e => e.Id).ToList();

        var rows = await db.AccessEvents
            .Where(a => a.EmployeeId != null && empIds.Contains(a.EmployeeId.Value)
                        && a.PersonName != null && a.PersonName != "")
            .Select(a => new { EmpId = a.EmployeeId!.Value, a.PersonName })
            .ToListAsync();

        var byEmp = rows.GroupBy(r => r.EmpId).ToDictionary(g => g.Key, g => g.ToList());
        var changed = false;
        foreach (var emp in employees)
        {
            if (!byEmp.TryGetValue(emp.Id, out var list) || list.Count == 0) continue;
            var groups = list.GroupBy(r => r.PersonName!)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count).ToList();
            var total = groups.Sum(x => x.Count);
            if (groups[0].Count * 100 / total >= 75) { emp.DeviceName = groups[0].Name; changed = true; }
        }
        if (changed) await db.SaveChangesAsync();
    }

    /// <summary>
    /// "Təhlükəsizlik" bölməsini + "Təhlükəsizlik məsulu" rolunu (bütün müəssisələri oxu-görünüşü)
    /// və Administratora bölmə icazəsini bir dəfə əlavə edir. Mövcud bazaya da tətbiq olunur.
    /// </summary>
    public static async Task SeedSecuritySectionAsync(AppDbContext db)
    {
        // 1. Bölmə
        var section = await db.Sections.FirstOrDefaultAsync(s => s.Code == "security");
        if (section is null)
        {
            section = new Section { Code = "security", Name = "Təhlükəsizlik", SortOrder = 13 };
            db.Sections.Add(section);
            await db.SaveChangesAsync();
        }

        // Administrator bu bölməyə tam icazə (yoxdursa)
        var admin = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (admin is not null && !await db.RolePermissions.AnyAsync(rp => rp.RoleId == admin.Id && rp.SectionId == section.Id))
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = admin.Id, SectionId = section.Id, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true
            });

        // 2. Təhlükəsizlik məsulu rolu (oxu-yönümlü, bütün müəssisələr)
        if (!await db.Roles.AnyAsync(r => r.IsSecurity))
        {
            var role = new Role
            {
                Name = "Təhlükəsizlik məsulu",
                Description = "Bütün müəssisələrin giriş-çıxış nəzarəti (oxu-yönümlü)",
                IsSystem = false,
                IsSecurity = true
            };
            db.Roles.Add(role);
            await db.SaveChangesAsync();

            // Yalnız izləmə (view) — monitorinq bölmələri üzrə (mövcud olanlar).
            var viewCodes = new[] { "dashboard", "security", "access_events", "guests", "history", "active_permits", "reports" };
            var secSections = await db.Sections.Where(s => viewCodes.Contains(s.Code)).ToListAsync();
            foreach (var s in secSections)
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id, SectionId = s.Id, CanView = true, CanAdd = false, CanEdit = false, CanDelete = false
                });
        }

        // Təhlükəsizlik roluna "settings" icazəsi (Mərkəz + Gəliş məqsədi idarəetməsi üçün) — idempotent.
        var securityRole = await db.Roles.FirstOrDefaultAsync(r => r.IsSecurity);
        var settingsSection = await db.Sections.FirstOrDefaultAsync(s => s.Code == "settings");
        if (securityRole is not null && settingsSection is not null
            && !await db.RolePermissions.AnyAsync(rp => rp.RoleId == securityRole.Id && rp.SectionId == settingsSection.Id))
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = securityRole.Id, SectionId = settingsSection.Id, CanView = true, CanAdd = true, CanEdit = true, CanDelete = true
            });

        await db.SaveChangesAsync();
    }

    /// <summary>Parametrlər səhifəsinin tabları üçün alt-bölmələri (ayrıca icazə kodları) yaradır.
    /// Beləcə rol matrisində məsələn "Cihazlar" bağlanıb "Mərtəbələr" açıq saxlanıla bilir.
    /// Miqrasiya: hər rola öz cari "settings" icazəsi köçürülür — heç kim mövcud çıxışını itirmir.
    /// İdempotentdir; yalnız çatışmayan bölmələri əlavə edir.</summary>
    public static async Task SeedSettingsSubSectionsAsync(AppDbContext db)
    {
        var defs = new (string Code, string Name, int Sort)[]
        {
            ("set_centers",     "Parametrlər: Mərkəzlər",          20),
            ("set_departments", "Parametrlər: Şöbələr",            21),
            ("set_positions",   "Parametrlər: Vəzifələr",          22),
            ("set_hosts",       "Parametrlər: Qəbul edən şəxslər", 23),
            ("set_purposes",    "Parametrlər: Gəliş məqsədləri",   24),
            ("set_floors",      "Parametrlər: Mərtəbələr",         25),
            ("set_devices",     "Parametrlər: Cihazlar",           26),
            ("set_timetables",  "Parametrlər: İş cədvəlləri",      27),
        };

        var codes = defs.Select(d => d.Code).ToList();
        var existing = await db.Sections.Where(s => codes.Contains(s.Code)).Select(s => s.Code).ToListAsync();
        var missing = defs.Where(d => !existing.Contains(d.Code)).ToList();
        if (missing.Count == 0) return;

        var added = missing.Select(d => new Section { Code = d.Code, Name = d.Name, SortOrder = d.Sort }).ToList();
        db.Sections.AddRange(added);
        await db.SaveChangesAsync();

        // Mövcud rollara "settings" icazəsinin eynisini ver (Administrator — hamısı açıq).
        var settingsSection = await db.Sections.FirstOrDefaultAsync(s => s.Code == "settings");
        var roles = await db.Roles.Include(r => r.Permissions).ToListAsync();
        foreach (var role in roles)
        {
            var src = settingsSection is null
                ? null
                : role.Permissions.FirstOrDefault(p => p.SectionId == settingsSection.Id);

            foreach (var sec in added)
            {
                if (role.Permissions.Any(p => p.SectionId == sec.Id)) continue;
                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    SectionId = sec.Id,
                    CanView = role.IsSystem || (src?.CanView ?? false),
                    CanAdd = role.IsSystem || (src?.CanAdd ?? false),
                    CanEdit = role.IsSystem || (src?.CanEdit ?? false),
                    CanDelete = role.IsSystem || (src?.CanDelete ?? false)
                });
            }
        }
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Çoxkiracılığa keçid: CompanyId-si boş olan mövcud strukturu (mərkəz/mərtəbə/cihaz/keçid nöqtəsi)
    /// və qeyri-qlobal istifadəçiləri default (ilk) şirkətə bağlayır. Bir dəfə tətbiq olunur.
    /// </summary>
    public static async Task SeedTenantBackfillAsync(AppDbContext db)
    {
        var company = await db.Companies.OrderBy(c => c.Id).FirstOrDefaultAsync();
        if (company is null) return;
        // Bir dəfəlik: hər hansı istifadəçiyə artıq şirkət təyin olunubsa (çoxkiracılığa keçilib),
        // təkrar süpürmə etmə — yoxsa qlobal adminin yeni (null) loqları da default şirkətə düşər.
        if (await db.Users.AnyAsync(u => u.CompanyId != null)) return;
        var cid = company.Id;
        var changed = false;

        foreach (var c in await db.Centers.Where(x => x.CompanyId == null).ToListAsync()) { c.CompanyId = cid; changed = true; }
        foreach (var f in await db.Floors.Where(x => x.CompanyId == null).ToListAsync()) { f.CompanyId = cid; changed = true; }
        foreach (var d in await db.Devices.Where(x => x.CompanyId == null).ToListAsync()) { d.CompanyId = cid; changed = true; }
        foreach (var a in await db.AccessPoints.Where(x => x.CompanyId == null).ToListAsync()) { a.CompanyId = cid; changed = true; }
        // Qonaq domeni + audit loqları — mövcud data default şirkətə (yoxsa şirkət useri görməz).
        foreach (var h in await db.Hosts.Where(x => x.CompanyId == null).ToListAsync()) { h.CompanyId = cid; changed = true; }
        foreach (var v in await db.Visits.Where(x => x.CompanyId == null).ToListAsync()) { v.CompanyId = cid; changed = true; }
        foreach (var s in await db.SystemLogs.Where(x => x.CompanyId == null).ToListAsync()) { s.CompanyId = cid; changed = true; }
        // Qeyri-qlobal (qorunmayan) istifadəçilər default şirkətə; qlobal admin (IsProtected) null qalır.
        foreach (var u in await db.Users.Where(x => x.CompanyId == null && !x.IsProtected).ToListAsync()) { u.CompanyId = cid; changed = true; }

        if (changed) await db.SaveChangesAsync();
    }

    /// <summary>"Keçid hadisələri" bölməsini (Section) və Administrator roluna icazəsini bir dəfə əlavə edir.</summary>
    public static async Task SeedAccessEventsSectionAsync(AppDbContext db)
    {
        if (await db.Sections.AnyAsync(s => s.Code == "access_events")) return;
        var section = new Section { Code = "access_events", Name = "Keçid hadisələri", SortOrder = 12 };
        db.Sections.Add(section);
        await db.SaveChangesAsync();

        var adminRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Administrator");
        if (adminRole is not null)
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id, SectionId = section.Id,
                CanView = true, CanAdd = true, CanEdit = true, CanDelete = true
            });
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Hər cihaz üçün (AccessPointId boşdursa) 1:1 keçid nöqtəsi yaradır və bağlayır.
    /// Mövcud bazaya da bir dəfə tətbiq olunur.
    /// </summary>
    public static async Task SeedAccessPointsAsync(AppDbContext db)
    {
        var devices = await db.Devices
            .Include(d => d.Floor)
            .Where(d => d.AccessPointId == null)
            .ToListAsync();
        if (devices.Count == 0) return;

        foreach (var d in devices)
        {
            var ap = new AccessPoint
            {
                Name = d.Name,
                FloorId = d.FloorId,
                CenterId = d.Floor?.CenterId,
                Direction = d.Direction,
                // Default: Door (generic) — köhnə status evristikası qorunur. İstifadəçi
                // sonra Əsas giriş / Mərtəbə girişi seçib dəqiq məntiqi aktivləşdirə bilər.
                PointType = PointType.Door,
                IsActive = true
            };
            db.AccessPoints.Add(ap);
            d.AccessPoint = ap;
        }
        await db.SaveChangesAsync();
    }

    public static async Task SeedDevicesAsync(AppDbContext db)
    {
        if (!await db.Floors.AnyAsync())
        {
            var floor = new Floor { Name = "1-ci mərtəbə" };
            db.Floors.Add(floor);
            db.Devices.Add(new Device
            {
                Name = "1-ci mərtəbə - Giriş",
                Ip = "10.130.0.189",
                Port = 80,
                Floor = floor,
                Direction = DeviceDirection.Entry,
                DoorNo = 1,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        // Real test kartı (cihazdan oxunan Mifare UID) — yoxdursa əlavə et.
        if (!await db.Cards.AnyAsync(c => c.CardNo == "2903913593"))
        {
            db.Cards.Add(new Card
            {
                CardNo = "2903913593",
                Note = "Real test kartı (Mifare UID)",
                Status = CardStatus.Free,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }
    }
}
