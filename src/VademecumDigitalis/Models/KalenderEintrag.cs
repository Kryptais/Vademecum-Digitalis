using System.Text.Json.Serialization;

namespace VademecumDigitalis.Models;

/// <summary>
/// Ein Kalendereintrag (Geburtstag, Fest, ...) der im aventurischen Kalender angezeigt wird.
/// </summary>
public class KalenderEintrag
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Titel { get; set; } = string.Empty;

    /// <summary>Tag des Monats (1-30 bzw. 1-5 für Namenlose Tage).</summary>
    public int EintragTag { get; set; }

    /// <summary>Monat (1-13). 13 = Namenlose Tage.</summary>
    public int EintragMonat { get; set; }

    /// <summary>Jahr (nur bei Einmalig-Einträgen). 0 = jährlich wiederkehrend.</summary>
    public int EintragJahr { get; set; }

    /// <summary>True = jedes Jahr, False = nur im angegebenen Jahr.</summary>
    public bool IstJaehrlich { get; set; } = true;

    [JsonIgnore]
    public string BeschreibungKurz
    {
        get
        {
            var monatName = BoronKalender.GetMonat(EintragMonat)?.Name ?? "?";
            return IstJaehrlich
                ? $"↻ {EintragTag}. {monatName} (jährl.)"
                : $"{EintragTag}. {monatName} {EintragJahr} BF";
        }
    }

    /// <summary>Gibt true zurück, wenn dieser Eintrag am gegebenen Datum zutrifft.</summary>
    public bool TrifftAn(BoronDatum datum)
    {
        if (datum.Tag != EintragTag || datum.Monat != EintragMonat) return false;
        return IstJaehrlich || datum.Jahr == EintragJahr;
    }
}
