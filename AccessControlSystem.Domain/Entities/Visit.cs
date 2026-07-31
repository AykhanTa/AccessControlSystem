using AccessControlSystem.Domain.Common;
using AccessControlSystem.Domain.Enums;

namespace AccessControlSystem.Domain.Entities;

/// <summary>Ziyarət — bir giriş sessiyası.</summary>
public class Visit : BaseEntity
{
    public long GuestId { get; set; }
    public Guest Guest { get; set; } = null!;

    public long HostId { get; set; }              // qəbul edən şəxs
    public Host Host { get; set; } = null!;

    public PassType PassType { get; set; }        // buraxılış növü

    public long? CardId { get; set; }             // PassType=Card olduqda dolur
    public Card? Card { get; set; }

    public string? QrToken { get; set; }          // PassType=Qr olduqda (unikal token)

    /// <summary>Cihaza yazılan vahid nömrə: employeeNo = cardNo = QR mətni.
    /// Kartlıda kartın nömrəsi, QR-də generasiya olunan numerik.</summary>
    public string? AccessNumber { get; set; }

    public DateTime ArrivalAt { get; set; }        // gəliş tarixi/vaxtı (planlaşdırılmış)
    public DateTime? ExpectedExitAt { get; set; }  // çıxış (proqnoz)
    public DateTime? CheckedInAt { get; set; }     // nəzarətçi kart təyin etdiyi an
    public DateTime? ActualExitAt { get; set; }    // faktiki çıxış

    public VisitStatus Status { get; set; } = VisitStatus.Planned;
    public string? Note { get; set; }
    public long? CreatedBy { get; set; }           // ziyarəti qeydə alan istifadəçi

    /// <summary>Aid olduğu şirkət (çoxkiracılı təcrid — qonaq həmin şirkətə gəlir).</summary>
    public long? CompanyId { get; set; }

    public ICollection<VisitArea> VisitAreas { get; set; } = new List<VisitArea>();
    public ICollection<VisitPurpose> VisitPurposes { get; set; } = new List<VisitPurpose>();
    public ICollection<VisitFloor> VisitFloors { get; set; } = new List<VisitFloor>();
    public ICollection<DeviceEnrollment> DeviceEnrollments { get; set; } = new List<DeviceEnrollment>();
}
