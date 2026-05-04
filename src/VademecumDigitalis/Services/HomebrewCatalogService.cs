using System.Text.Json;
using System.Text.Json.Serialization;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>
/// Persistiert benutzerdefinierte Homebrew-Vorteile/Nachteile als JSON im AppData-Verzeichnis.
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

    /// <summary>Lädt alle Homebrew-Einträge aus der Datei (leere Liste wenn keine vorhanden).</summary>
    public async Task<List<VorteilNachteil>> LoadAsync()
    {
        try
        {
            var path = FilePath;
            if (!File.Exists(path))
                return [];

            var json = await File.ReadAllTextAsync(path);
            return JsonSerializer.Deserialize<List<VorteilNachteil>>(json, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalog] Fehler beim Laden: {ex.Message}");
            return [];
        }
    }

    /// <summary>Speichert die komplette Homebrew-Liste.</summary>
    public async Task SaveAllAsync(IEnumerable<VorteilNachteil> entries)
    {
        try
        {
            var json = JsonSerializer.Serialize(entries.ToList(), JsonOptions);
            var dir = Path.GetDirectoryName(FilePath)!;
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(FilePath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalog] Fehler beim Speichern: {ex.Message}");
        }
    }

    /// <summary>Fügt einen neuen Eintrag hinzu und speichert.</summary>
    public async Task AddAsync(VorteilNachteil entry)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] Adding entry: Id={entry.Id}, Effects={entry.ExplicitEffects.Count}");
            var all = await LoadAsync();
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] Loaded {all.Count} existing entries");
            all.Add(entry);
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] Now have {all.Count} entries, saving...");
            await SaveAllAsync(all);
            System.Diagnostics.Debug.WriteLine($"[HomebrewCatalogService.AddAsync] ✓ Saved successfully");
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
        var all = await LoadAsync();
        var index = all.FindIndex(e => e.Id == entry.Id);
        if (index >= 0)
            all[index] = entry;
        else
            all.Add(entry);
        await SaveAllAsync(all);
    }

    /// <summary>Entfernt einen Eintrag per Id und speichert.</summary>
    public async Task DeleteAsync(string id)
    {
        var all = await LoadAsync();
        all.RemoveAll(e => e.Id == id);
        await SaveAllAsync(all);
    }
}
