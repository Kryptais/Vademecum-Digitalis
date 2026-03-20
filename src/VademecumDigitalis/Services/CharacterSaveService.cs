using System.Text.Json;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

public interface ICharacterSaveService
{
    Task SaveCharacterAsync(CharacterSheet character, string filename);
    Task<CharacterSheet?> LoadCharacterAsync(string filePath);
    Task<IEnumerable<string>> GetSavedCharactersAsync();
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

    public async Task SaveCharacterAsync(CharacterSheet character, string filename)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Filename cannot be empty", nameof(filename));
            }

            var fileName = Path.GetFileNameWithoutExtension(filename) + ".json";
            var filePath = Path.Combine(_savePath, fileName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(character, options);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save character: {ex.Message}", ex);
        }
    }

    public async Task<CharacterSheet?> LoadCharacterAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Character file not found: {filePath}");
            }

            var json = await File.ReadAllTextAsync(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<CharacterSheet>(json, options);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to load character: {ex.Message}", ex);
        }
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

    public string SavePath => _savePath;
}
