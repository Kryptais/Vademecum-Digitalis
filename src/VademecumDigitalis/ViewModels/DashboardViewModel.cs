using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using VademecumDigitalis.Models;
using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private readonly CharacterSaveService _saveService;
    private readonly MainPageViewModel _mainVm;
    private bool _isLoading;
    private string _statusMessage = string.Empty;

    public DashboardViewModel(CharacterSaveService saveService, MainPageViewModel mainVm)
    {
        _saveService = saveService;
        _mainVm = mainVm;
    }

    public ObservableCollection<CharacterSummary> Characters { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; Notify(); Notify(nameof(HasNoCharacters)); } }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set { if (_statusMessage != value) { _statusMessage = value; Notify(); Notify(nameof(HasStatus)); } }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);
    public bool HasNoCharacters => !IsLoading && Characters.Count == 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadCharactersAsync()
    {
        IsLoading = true;
        StatusMessage = string.Empty;
        Characters.Clear();

        var summaries = await _saveService.GetCharacterSummariesAsync();
        foreach (var s in summaries)
            Characters.Add(s);

        IsLoading = false;
    }

    /// <summary>
    /// Lädt einen Charakter in die aktive Session und gibt true zurück bei Erfolg.
    /// </summary>
    public async Task<bool> LoadCharacterAsync(CharacterSummary summary)
    {
        try
        {
            IsLoading = true;
            var data = await _saveService.LoadCharacterAsync(summary.FilePath);
            if (data is null)
            {
                StatusMessage = "Charakter konnte nicht geladen werden.";
                return false;
            }

            await _mainVm.LoadFromCharacterSheetDataAsync(data, summary.FileName);
            StatusMessage = string.Empty;
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler: {ex.Message}";
            return false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Legt einen neuen, leeren Charakter an und setzt die Session zurück.</summary>
    public void CreateNewCharacter()
    {
        _mainVm.ResetToNewCharacter();
        StatusMessage = string.Empty;
    }

    public async Task DeleteCharacterAsync(CharacterSummary summary)
    {
        await _saveService.DeleteCharacterAsync(summary.FileName);
        Characters.Remove(summary);
    }

    /// <summary>Exportiert den aktuell geladenen Charakter als JSON-Datei.</summary>
    public async Task ExportCurrentCharacterAsync()
    {
        try
        {
            var name = _mainVm.Name;
            if (string.IsNullOrWhiteSpace(name)) name = "Charakter";

            var safeName = string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            var fileName = $"{safeName}_export.json";
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);

            var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var json = JsonSerializer.Serialize(_mainVm.ToCharacterSheetData(), options);
            await File.WriteAllTextAsync(tempPath, json);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = $"{name} exportieren",
                File = new ShareFile(tempPath)
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export fehlgeschlagen: {ex.Message}";
        }
    }

    public async Task<bool> ImportCharacterAsync()
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Charakter importieren",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, [".json"] },
                    { DevicePlatform.Android, ["application/json"] },
                    { DevicePlatform.iOS, ["public.json"] },
                    { DevicePlatform.MacCatalyst, ["public.json"] }
                })
            });

            if (result is null) return false;

            var json = await File.ReadAllTextAsync(result.FullPath);
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

            CharacterSheetData? data = null;
            try { data = JsonSerializer.Deserialize<CharacterSheetData>(json, options); } catch { }
            if (data?.Sheet is null || string.IsNullOrEmpty(data.Sheet.Name))
            {
                var sheet = JsonSerializer.Deserialize<CharacterSheet>(json, options);
                if (sheet is null) { StatusMessage = "Ungültige Charakterdatei."; return false; }
                data = new CharacterSheetData { Sheet = sheet };
            }

            var saveName = string.IsNullOrWhiteSpace(data.Sheet.Name)
                ? Path.GetFileNameWithoutExtension(result.FileName)
                : data.Sheet.Name;

            await _saveService.SaveCharacterAsync(data, saveName);
            await LoadCharactersAsync();
            StatusMessage = $"\"{saveName}\" importiert.";
            return true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import fehlgeschlagen: {ex.Message}";
            return false;
        }
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
