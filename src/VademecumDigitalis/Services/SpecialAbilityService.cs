using System.Text.Json;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>
/// Service für Sonderfertigkeiten: Katalog laden, Suche, Validierung und AP-Berechnung.
/// </summary>
public class SpecialAbilityService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private List<SpecialAbility> _catalog = [];
    private bool _loaded;

    /// <summary>Der geladene Katalog aller profanen Sonderfertigkeiten.</summary>
    public IReadOnlyList<SpecialAbility> Catalog => _catalog;

    /// <summary>Lädt den Katalog aus der eingebetteten profane_sf.json Ressource.</summary>
    public async Task LoadCatalogAsync()
    {
        if (_loaded) return;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("profane_sf.json");
            var items = await JsonSerializer.DeserializeAsync<List<SpecialAbility>>(stream, JsonOptions);
            _catalog = items ?? [];
            _loaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading SF catalog: {ex.Message}");
            _catalog = [];
        }
    }

    /// <summary>Sucht im Katalog nach Name (case-insensitive Teilstring).</summary>
    public IReadOnlyList<SpecialAbility> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _catalog;

        return _catalog
            .Where(sf => sf.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Sucht im Katalog nach Kategorie.</summary>
    public IReadOnlyList<SpecialAbility> SearchByCategory(SpecialAbilityCategory category)
    {
        return _catalog.Where(sf => sf.Kategorie == category).ToList();
    }

    /// <summary>Findet einen Katalogeintrag per ID.</summary>
    public SpecialAbility? FindById(string id)
    {
        return _catalog.FirstOrDefault(sf => sf.Id == id);
    }

    /// <summary>
    /// Prüft, ob der Charakter die Sonderfertigkeit auf der angegebenen Stufe erwerben kann.
    /// Gibt einen leeren String zurück, wenn alles erfüllt ist, sonst eine Fehlermeldung.
    /// </summary>
    public string CanAcquire(SpecialAbility sf, int targetLevel, IReadOnlyDictionary<string, int>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(sf);

        if (targetLevel < 1 || targetLevel > sf.MaxStufe)
            return $"Stufe {targetLevel} ist ungültig (max. {sf.MaxStufe}).";

        int index = targetLevel - 1;
        if (sf.VoraussetzungenProStufe.Count > index)
        {
            var reqs = sf.VoraussetzungenProStufe[index];
            foreach (var req in reqs)
            {
                if (attributes != null && attributes.TryGetValue(req.Name, out int currentValue))
                {
                    if (currentValue < req.MinValue)
                        return $"Voraussetzung nicht erfüllt: {req} (aktuell: {currentValue}).";
                }
            }
        }

        return string.Empty;
    }

    /// <summary>Ermittelt die AP-Kosten für die nächste Stufe.</summary>
    public int GetNextLevelCost(SpecialAbility sf, int currentLevel)
    {
        ArgumentNullException.ThrowIfNull(sf);

        int nextIndex = currentLevel; // currentLevel ist 0-basiert für den nächsten Index
        if (nextIndex < 0 || nextIndex >= sf.ApKostenProStufe.Count)
            return 0;

        return sf.ApKostenProStufe[nextIndex];
    }

    /// <summary>Berechnet die Gesamt-AP-Kosten für eine SF bis zur angegebenen Stufe.</summary>
    public int GetTotalCost(SpecialAbility sf, int upToLevel)
    {
        ArgumentNullException.ThrowIfNull(sf);

        int total = 0;
        for (int i = 0; i < upToLevel && i < sf.ApKostenProStufe.Count; i++)
        {
            total += sf.ApKostenProStufe[i];
        }
        return total;
    }

    /// <summary>Berechnet die Gesamt-AP-Kosten aller erworbenen Sonderfertigkeiten.</summary>
    public int CalculateTotalApCost(IEnumerable<CharakterSonderfertigkeitEintrag> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        int total = 0;
        foreach (var entry in entries)
        {
            var catalogEntry = FindById(entry.SfId);
            if (catalogEntry != null)
            {
                total += GetTotalCost(catalogEntry, entry.Stufe);
            }
        }
        return total;
    }

    /// <summary>Erstellt einen Charakter-Eintrag aus einem Katalogeintrag.</summary>
    public static CharakterSonderfertigkeitEintrag CreateEntry(SpecialAbility sf, int stufe = 1, bool forceAdd = false)
    {
        ArgumentNullException.ThrowIfNull(sf);

        return new CharakterSonderfertigkeitEintrag
        {
            SfId = sf.Id,
            Name = sf.Name,
            Kategorie = sf.Kategorie,
            Stufe = stufe,
            MaxStufe = sf.MaxStufe,
            ForceAdded = forceAdd
        };
    }

    /// <summary>Fügt eine benutzerdefinierte Homebrew-SF zum Katalog hinzu.</summary>
    public void AddHomebrewEntry(SpecialAbility homebrew)
    {
        ArgumentNullException.ThrowIfNull(homebrew);
        _catalog.Add(homebrew);
    }
}
