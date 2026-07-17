namespace AccessControlSystem.Application.DTOs;

/// <summary>Ad → say cütü (məqsəd, ərazi, kart və s. bölgüləri üçün).</summary>
public class LabelCount
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>İllik hesabat göstəriciləri (ziyarətlərdən hesablanır).</summary>
public class ReportDto
{
    public int Year { get; set; }

    // Yekun göstəricilər (plitələr)
    public int TotalVisits { get; set; }   // Ümumi ziyarət
    public int UniqueGuests { get; set; }  // Unikal qonaq
    public int Entries { get; set; }       // Qeydə alınmış giriş
    public int Exits { get; set; }         // Qeydə alınmış çıxış
    public int CardUse { get; set; }       // Kart istifadəsi
    public int QrUse { get; set; }         // QR istifadəsi
    public int Inside { get; set; }        // Hazırda binada
    public int Late { get; set; }          // Gecikmiş çıxış

    public int ExitRatePercent { get; set; }   // Nəzarət göstəricisi
    public int AvgStayMinutes { get; set; }     // Orta qalma müddəti (dəq)

    public List<LabelCount> Months { get; set; } = new();     // Aylıq jurnal (12 ay)
    public List<LabelCount> Purposes { get; set; } = new();   // Gəliş məqsədləri
    public List<LabelCount> Hosts { get; set; } = new();      // Qəbul edənlər
    public List<LabelCount> Areas { get; set; } = new();      // Ərazilər
    public List<LabelCount> Cards { get; set; } = new();      // Kartların istifadə tezliyi
}
