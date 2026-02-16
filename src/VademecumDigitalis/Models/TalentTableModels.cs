namespace VademecumDigitalis.Models;

public class TalentGroup
{
    public TalentGroup(string gruppe, IEnumerable<TalentRow> eintraege)
    {
        Gruppe = gruppe;
        Eintraege = eintraege.ToList();
    }

    public string Gruppe { get; }

    public List<TalentRow> Eintraege { get; }
}

public class TalentRow
{
    public string Talent { get; set; } = string.Empty;
    public string Steigerungsfaktor { get; set; } = string.Empty;
    public string Probe1 { get; set; } = string.Empty;
    public string Probe2 { get; set; } = string.Empty;
    public string Probe3 { get; set; } = string.Empty;
    public string Belastungseinfluss { get; set; } = string.Empty;
    public string Fw { get; set; } = string.Empty;
    public string Anmerkung { get; set; } = string.Empty;
}
