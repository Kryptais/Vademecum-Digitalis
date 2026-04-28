using System.Text.Json;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

public sealed record CharacterSummary(
    string FileName,
    string CharacterName,
    string Spieler,
    string Spezies,
    DateTime LastModified,
    string FilePath);

public interface ICharacterSaveService
{
    Task SaveCharacterAsync(CharacterSheetData data, string filename);
    Task<CharacterSheetData?> LoadCharacterAsync(string filePath);
    Task<IEnumerable<string>> GetSavedCharactersAsync();
    Task<IEnumerable<CharacterSummary>> GetCharacterSummariesAsync();
    Task DeleteCharacterAsync(string filename);
    string GetCharacterPath(string filename);
}

public class CharacterSaveService : ICharacterSaveService
{
    private readonly string _savePath;
    private const string DefaultFileName = "Characters";

    public CharacterSaveService()
    {
        _savePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "VademecumDigitalis"
        );

        if (!Directory.Exists(_savePath))
        {
            Directory.CreateDirectory(_savePath);
        }
    }

    public async Task SaveCharacterAsync(CharacterSheetData data, string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename cannot be empty", nameof(filename));

        var filePath = Path.Combine(_savePath, Path.GetFileNameWithoutExtension(filename) + ".json");
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var json = JsonSerializer.Serialize(data, options);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<CharacterSheetData?> LoadCharacterAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Charakterdatei nicht gefunden: {filePath}");

        var json = await File.ReadAllTextAsync(filePath);
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        // Versuche zunächst das aktuelle Format (CharacterSheetData)
        try
        {
            var data = JsonSerializer.Deserialize<CharacterSheetData>(json, options);
            if (data?.Sheet is not null && !string.IsNullOrEmpty(data.Sheet.Name))
                return data;
        }
        catch { }

        // Fallback: altes Format (nur CharacterSheet)
        var sheet = JsonSerializer.Deserialize<CharacterSheet>(json, options);
        return sheet is null ? null : new CharacterSheetData { Sheet = sheet };
    }

    public async Task<IEnumerable<string>> GetSavedCharactersAsync()
    {
        return await Task.Run(() =>
        {
            if (!Directory.Exists(_savePath))
            {
                return Enumerable.Empty<string>();
            }

            return Directory.GetFiles(_savePath, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .Where(name => name != null)
                .Cast<string>()
                .OrderByDescending(name => 
                {
                    var filePath = Path.Combine(_savePath, $"{name}.json");
                    return File.GetLastWriteTime(filePath);
                });
        });
    }

    public string GetCharacterPath(string filename)
    {
        var fileName = Path.GetFileNameWithoutExtension(filename) + ".json";
        return Path.Combine(_savePath, fileName);
    }

    public async Task<IEnumerable<CharacterSummary>> GetCharacterSummariesAsync()
    {
        if (!Directory.Exists(_savePath))
            return [];

        var files = Directory.GetFiles(_savePath, "*.json")
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();

        var summaries = new List<CharacterSummary>();
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        foreach (var filePath in files)
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                CharacterSheet? sheet = null;
                try { sheet = (JsonSerializer.Deserialize<CharacterSheetData>(json, options))?.Sheet; } catch { }
                sheet ??= JsonSerializer.Deserialize<CharacterSheet>(json, options);
                var name = sheet?.Name ?? Path.GetFileNameWithoutExtension(filePath);
                summaries.Add(new CharacterSummary(
                    FileName: Path.GetFileNameWithoutExtension(filePath),
                    CharacterName: string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(filePath) : name,
                    Spieler: sheet?.Spieler ?? string.Empty,
                    Spezies: sheet?.Spezies ?? string.Empty,
                    LastModified: File.GetLastWriteTime(filePath),
                    FilePath: filePath));
            }
            catch
            {
                summaries.Add(new CharacterSummary(
                    FileName: Path.GetFileNameWithoutExtension(filePath),
                    CharacterName: Path.GetFileNameWithoutExtension(filePath),
                    Spieler: string.Empty,
                    Spezies: string.Empty,
                    LastModified: File.GetLastWriteTime(filePath),
                    FilePath: filePath));
            }
        }

        return summaries;
    }

    public Task DeleteCharacterAsync(string filename)
    {
        var filePath = GetCharacterPath(filename);
        if (File.Exists(filePath))
            File.Delete(filePath);
        return Task.CompletedTask;
    }

    public string SavePath => _savePath;
}
