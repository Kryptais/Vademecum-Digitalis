using VademecumDigitalis.Services;

namespace VademecumDigitalis.ViewModels;

public static class CharacterSheetSession
{
    private static MainPageViewModel? _current;

    /// <summary>
    /// Initialisiert die Session mit einem ViewModel aus DI.
    /// </summary>
    public static void Initialize(MainPageViewModel vm)
    {
        _current = vm;
    }

    public static MainPageViewModel Current => _current ?? throw new InvalidOperationException("CharacterSheetSession has not been initialized. Call Initialize() first.");
}
