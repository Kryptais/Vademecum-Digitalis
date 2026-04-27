using System.Text.Json;
using Microsoft.Maui.Storage;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>
/// DTO für die separate Persistierung der strukturierten Vorteile/Nachteile.
/// </summary>
public class AdvantagesData
{
    public List<Vorteil> Vorteile { get; set; } = [];
    public List<Vorteil> Nachteile { get; set; } = [];
}

/// <summary>
/// Service für die Verwaltung von DSA-5-Vorteilen und -Nachteilen:
/// Persistierung, AP-Berechnung und Katalogzugriff.
/// </summary>
public class AdvantagesService
{
    private const string FileName = "advantages_data.json";

    private string FilePath => Path.Combine(FileSystem.AppDataDirectory, FileName);

    // ── Persistenz ────────────────────────────────────────────────────────────────

    /// <summary>Speichert Vorteile und Nachteile in einer JSON-Datei.</summary>
    public async Task SaveAsync(IEnumerable<Vorteil> vorteile, IEnumerable<Vorteil> nachteile)
    {
        try
        {
            var data = new AdvantagesData
            {
                Vorteile = vorteile.ToList(),
                Nachteile = nachteile.ToList()
            };
            var options = new JsonSerializerOptions { WriteIndented = true };
            using var stream = File.Create(FilePath);
            await JsonSerializer.SerializeAsync(stream, data, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdvantagesService] Fehler beim Speichern: {ex.Message}");
        }
    }

    /// <summary>Lädt Vorteile und Nachteile aus der JSON-Datei.</summary>
    public async Task<AdvantagesData> LoadAsync()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AdvantagesData();

            using var stream = File.OpenRead(FilePath);
            var data = await JsonSerializer.DeserializeAsync<AdvantagesData>(stream);
            return data ?? new AdvantagesData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdvantagesService] Fehler beim Laden: {ex.Message}");
            return new AdvantagesData();
        }
    }

    // ── AP-Berechnung ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Berechnet die gesamten AP-Kosten einer Vorteilsliste anhand des Katalogs.
    /// Bei unbekannten Einträgen wird 0 angesetzt.
    /// </summary>
    public int BerechneApKosten(IEnumerable<Vorteil> vorteile)
    {
        int gesamt = 0;
        foreach (var v in vorteile)
        {
            var eintrag = VorteilKatalog.FindByName(v.Name);
            if (eintrag is null) continue;

            int stufe = eintrag.MaxStufe > 0 ? Math.Max(1, v.Stufe) : 1;
            gesamt += eintrag.ApKostenProStufe * stufe;
        }
        return gesamt;
    }

    /// <summary>
    /// Berechnet die gesamten AP-Einnahmen einer Nachteilsliste anhand des Katalogs.
    /// Gibt einen positiven Wert zurück (die tatsächlichen AP wurden gewonnen).
    /// </summary>
    public int BerechneApEinnahmen(IEnumerable<Vorteil> nachteile)
    {
        int gesamt = 0;
        foreach (var n in nachteile)
        {
            var eintrag = VorteilKatalog.FindByName(n.Name);
            if (eintrag is null) continue;

            int stufe = eintrag.MaxStufe > 0 ? Math.Max(1, n.Stufe) : 1;
            gesamt += eintrag.ApKostenProStufe * stufe;
        }
        return gesamt;
    }
}
