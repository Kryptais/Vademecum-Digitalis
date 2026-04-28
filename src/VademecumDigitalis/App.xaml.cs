using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class App : Application
{
    private IServiceProvider? _services;

    public App(MainPageViewModel mainVm, IServiceProvider services)
    {
        InitializeComponent();

        _services = services;
        CharacterSheetSession.Initialize(mainVm);

        // Dashboard als Startseite setzen
        try
        {
            MainPage = services.GetRequiredService<DashboardPage>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[App] Dashboard konnte nicht geladen werden: {ex}");
            // Fallback: direkt zur Charakteransicht
            MainPage = services.GetRequiredService<AppShell>();
        }
    }

    /// <summary>Wechselt zur Charakteransicht (TabBar-Shell).</summary>
    public void SwitchToCharacterShell(IServiceProvider services)
    {
        MainPage = services.GetRequiredService<AppShell>();
    }

    /// <summary>Wechselt zurück zum Dashboard (z. B. aus Einstellungen).</summary>
    public void SwitchToDashboard(IServiceProvider services)
    {
        MainPage = services.GetRequiredService<DashboardPage>();
    }
}
