namespace AccessControlSystem.Domain.Enums;

/// <summary>Müvəqqəti kartın vəziyyəti.</summary>
public enum CardStatus
{
    /// <summary>Boş — istifadəyə hazır.</summary>
    Free = 0,
    /// <summary>Qonağa təyin edilib.</summary>
    Assigned = 1
}

/// <summary>Buraxılış növü.</summary>
public enum PassType
{
    /// <summary>Müvəqqəti kartla buraxılış.</summary>
    Card = 0,
    /// <summary>QR kod ilə buraxılış.</summary>
    Qr = 1
}

/// <summary>Sistem istifadəçisinin statusu.</summary>
public enum UserStatus
{
    Active = 0,
    Inactive = 1
}

/// <summary>Ziyarətin statusu.</summary>
public enum VisitStatus
{
    /// <summary>Binadadır.</summary>
    In = 0,
    /// <summary>Çıxıb.</summary>
    Out = 1,
    /// <summary>Gecikib.</summary>
    Late = 2
}
