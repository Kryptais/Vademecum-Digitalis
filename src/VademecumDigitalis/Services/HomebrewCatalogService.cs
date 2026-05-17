using System.Text.Json;
using System.Text.Json.Serialization;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>
/// Persistiert benutzerdefinierte Vorteile/Nachteile als JSON im AppData-Verzeichnis.
/// Speichert Offiziell- und Homebrew-Einträge in einer Datei, intern getrennt.
/// </summary>
public class HomebrewCatalogService
{
    private const string FileName = "homebrew_vorteile_nachteile.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private string FilePath => Path.Combine(FileSystem.AppDataDirectory, FileName);

    /// <summary>Wrapper-Format der User-Datei (neue Version).</summary>
    private sealed class UserCatalogFile
    {
        public List<VorteilNachteil> Offiziell { get; set; } = [];
        public List<VorteilNachteil> Homebrew { get; set; } = [];
    }

    /// <summary>Lädt alle User-Einträge (Offiziell + Homebrew) flach als eine Liste.</summary>
    public async Task<List<VorteilNachteil>> LoadAsync()
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
                return [];

            var json = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(json))
                return [];

            // Neues Format: Wrapper-Objekt
            try
            {
                var wrapper = JsonSerializer.Deserialize<UserCatalogFile>(json, JsonOptions);
                if (wrapper is not null && (wrapper.Offiziell.Count > 0 || wrapper.Homebrew.Count > 0))
                {
                    var combined = new List<VorteilNachteil>(wrapper.Offiziell.Count + wrapper.Homebrew.Count);
                    combined.AddRange(wrapper.Offiziell.Select(e => e with { IsHomebrew = false }));
                    combined.AddRange(wrapper.Homebrew.Select(e => e with { IsHomebrew = true }));
                    return combined;
                }
            }
            catch
            {
                // Wrapper-Format fehlgeschlagen → unten Legacy versuchen
            }

            // Legacy-Format: flache Liste
            try
            {
                var list = JsonSerializer.Deserialize<List<VorteilNachteil>>(json, JsonOptions);
                return list ?? [];
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[HomebrewCatalog] Legacy-Format-Fehler: {ex.Message}");
                return [];
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalog] Fehler beim Laden: {ex.Message}");
            return [];
        }
    }

    /// <summary>Speichert die komplette User-Liste, partitioniert nach IsHomebrew.</summary>
    public async Task SaveAllAsync(IEnumerable<VorteilNachteil> entries)
    {
        try
        {
            var list = entries.ToList();
            var wrapper = new UserCatalogFile
            {
                Offiziell = list.Where(e => !e.IsHomebrew).ToList(),
                Homebrew = list.Where(e => e.IsHomebrew).ToList()
            };
            var json = JsonSerializer.Serialize(wrapper, JsonOptions);
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalog] Fehler beim Speichern: {ex.Message}");
            throw;
        }
    }

    /// <summary>Fügt einen neuen Eintrag hinzu und speichert.</summary>
    public async Task AddAsync(VorteilNachteil entry)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] Adding entry: Id={entry.Id}, IsHomebrew={entry.IsHomebrew}, Effects={entry.ExplicitEffects.Count}");
            var all = await LoadAsync();
            // Bei doppelter Id den vorhandenen Eintrag ersetzen
            all.RemoveAll(e => e.Id == entry.Id);
            all.Add(entry);
            await SaveAllAsync(all);
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] ✓ Saved successfully ({all.Count} entries)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] ✗ ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>Aktualisiert einen bestehenden Eintrag (per Id-Match) und speichert.</summary>
    public async Task UpdateAsync(VorteilNachteil entry)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.UpdateAsync] Updating: Id={entry.Id}, IsHomebrew={entry.IsHomebrew}");
            var all = await LoadAsync();
            var index = all.FindIndex(e => e.Id == entry.Id);
            if (index >= 0)
                all[index] = entry;
            else
                all.Add(entry);
            await SaveAllAsync(all);
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.UpdateAsync] ✓ Saved successfully ({all.Count} entries)");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.UpdateAsync] ✗ ERROR: {ex.GetType().Name}: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.UpdateAsync] StackTrace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>Entfernt einen Eintrag per Id und speichert.</summary>
    public async Task DeleteAsync(string id)
    {
        var all = await LoadAsync();
        all.RemoveAll(e => e.Id == id);
        await SaveAllAsync(all);
    }
}
