using System.Text.Json;
using VademecumDigitalis.Models;
using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.Services;

/// <summary>
/// Service für Vorteile/Nachteile: Katalog laden, Suche, Validierung und AP-Berechnung.
/// </summary>
public class VorteilNachteilService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private List<VorteilNachteil> _catalog = [];
    private bool _loaded;

    /// <summary>Der geladene Katalog aller Vorteile und Nachteile.</summary>
    public IReadOnlyList<VorteilNachteil> Catalog => _catalog;

    /// <summary>Lädt den Katalog aus der eingebetteten vorteile_nachteile.json Ressource.</summary>
    public async Task LoadCatalogAsync()
    {
        if (_loaded) return;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("vorteile_nachteile.json");
            var items = await JsonSerializer.DeserializeAsync<List<VorteilNachteil>>(stream, JsonOptions);
            _catalog = items ?? [];
            _loaded = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading VN catalog: {ex.Message}");
            _catalog = [];
        }
    }

    /// <summary>Sucht im Katalog nach Name (case-insensitive Teilstring).</summary>
    public IReadOnlyList<VorteilNachteil> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _catalog;

        return _catalog
            .Where(vn =>
                vn.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                vn.Beschreibung.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                vn.Anmerkungen.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                vn.Kategorie.ToDisplayString().Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    /// <summary>Sucht im Katalog nach Kategorie.</summary>
    public IReadOnlyList<VorteilNachteil> SearchByCategory(VorteilNachteilKategorie kategorie)
    {
        return _catalog.Where(vn => vn.Kategorie == kategorie).ToList();
    }

    /// <summary>Findet einen Katalogeintrag per ID.</summary>
    public VorteilNachteil? FindById(string id)
    {
        return _catalog.FirstOrDefault(vn => vn.Id == id);
    }

    /// <summary>
    /// Prüft, ob der Charakter den Vorteil/Nachteil auf der angegebenen Stufe erwerben kann.
    /// Gibt einen leeren String zurück, wenn alles erfüllt ist, sonst eine Fehlermeldung.
    /// </summary>
    public string CanAcquire(VorteilNachteil vn, int targetLevel, IReadOnlyDictionary<string, int>? attributes = null)
    {
        ArgumentNullException.ThrowIfNull(vn);

        if (targetLevel < 1 || targetLevel > vn.MaxStufe)
            return $"Stufe {targetLevel} ist ungültig (max. {vn.MaxStufe}).";

        int index = targetLevel - 1;
        if (vn.VoraussetzungenProStufe.Count > index)
        {
            var reqs = vn.VoraussetzungenProStufe[index];
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
    public int GetNextLevelCost(VorteilNachteil vn, int currentLevel)
    {
        ArgumentNullException.ThrowIfNull(vn);

        int nextIndex = currentLevel;
        if (nextIndex < 0 || nextIndex >= vn.ApKostenProStufe.Count)
            return 0;

        return vn.ApKostenProStufe[nextIndex];
    }

    /// <summary>Berechnet die Gesamt-AP-Kosten für einen VN bis zur angegebenen Stufe.</summary>
    public int GetTotalCost(VorteilNachteil vn, int upToLevel)
    {
        ArgumentNullException.ThrowIfNull(vn);

        int total = 0;
        for (int i = 0; i < upToLevel && i < vn.ApKostenProStufe.Count; i++)
        {
            total += vn.ApKostenProStufe[i];
        }
        return total;
    }

    /// <summary>Berechnet die Gesamt-AP-Kosten aller erworbenen Vorteile/Nachteile.</summary>
    public int CalculateTotalApCost(IEnumerable<CharakterVorteilNachteilEintrag> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        int total = 0;
        foreach (var entry in entries)
        {
            if (entry.ApKosten != 0)
            {
                // Talent-gebundener VN: direkt gespeicherte AP-Kosten verwenden
                total += entry.ApKosten;
            }
            else
            {
                var catalogEntry = FindById(entry.VnId);
                if (catalogEntry != null)
                    total += GetTotalCost(catalogEntry, entry.Stufe);
            }
        }
        return total;
    }

    /// <summary>
    /// Erzeugt aktive Effektquellen aus den erworbenen VN-Einträgen.
    /// Bestehende ProbenModifikatoren werden zusätzlich als RuleEffect abgebildet.
    /// </summary>
    public IReadOnlyList<RuleEffectSource> CreateEffectSources(IEnumerable<CharakterVorteilNachteilEintrag> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var sources = new List<RuleEffectSource>();
        foreach (var entry in entries)
        {
            var catalogEntry = FindById(entry.VnId);
            if (catalogEntry is null)
                continue;

            var effects = catalogEntry.Effects
                .Concat(catalogEntry.ProbenModifikatoren.Select(ToRuleEffect))
                .ToList();

            if (effects.Count == 0)
                continue;

            sources.Add(new RuleEffectSource
            {
                SourceId = catalogEntry.Id,
                SourceName = catalogEntry.Name,
                Level = Math.Max(1, entry.Stufe),
                Effects = effects
            });
        }

        return sources;
    }

    /// <summary>Erstellt einen Charakter-Eintrag aus einem Katalogeintrag.</summary>
    public static CharakterVorteilNachteilEintrag CreateEntry(VorteilNachteil vn, int stufe = 1, bool forceAdd = false)
    {
        ArgumentNullException.ThrowIfNull(vn);

        return new CharakterVorteilNachteilEintrag
        {
            VnId = vn.Id,
            Name = vn.Name,
            Kategorie = vn.Kategorie,
            Stufe = stufe,
            MaxStufe = vn.MaxStufe,
            ForceAdded = forceAdd
        };
    }

    /// <summary>Fügt einen benutzerdefinierten Homebrew-Eintrag zum Katalog hinzu.</summary>
    public void AddHomebrewEntry(VorteilNachteil homebrew)
    {
        ArgumentNullException.ThrowIfNull(homebrew);
        _catalog.Add(homebrew);
    }

    private static RuleEffect ToRuleEffect(ProbenModifikator modifikator)
    {
        return new RuleEffect
        {
            Id = $"probe_{modifikator.Typ}_{NormalizeTarget(modifikator.Ziel)}",
            Kind = EffectKind.Modifier,
            Title = $"Probe: {modifikator.Ziel}",
            Target = modifikator.Typ switch
            {
                ModifikatorTyp.Attribut => $"check.attribute.{modifikator.Ziel}",
                ModifikatorTyp.Talent => $"check.talent.{modifikator.Ziel}",
                ModifikatorTyp.TalentGruppe => $"check.talentGroup.{modifikator.Ziel}",
                _ => modifikator.Ziel
            },
            Operation = ModifierOp.Add,
            Value = modifikator.Wert,
            PerLevel = true,
            Phase = ModifierPhase.Checks,
            Stacking = StackingRule.Stack
        };
    }

    private static string NormalizeTarget(string value)
    {
        return string.Concat(value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_'));
    }
}
