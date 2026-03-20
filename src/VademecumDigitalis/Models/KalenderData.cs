namespace VademecumDigitalis.Models;

/// <summary>
/// DTO für die Persistierung der aventurischen Kalender-Daten.
/// </summary>
public class KalenderData
{
    public BoronDatum AktuellesDatum { get; set; } = BoronDatum.Default;
    public string Notizen { get; set; } = string.Empty;
}
