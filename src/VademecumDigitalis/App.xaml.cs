using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class App : Application
{
    public App(MainPageViewModel mainVm, IServiceProvider services)
    {
        InitializeComponent();

        CharacterSheetSession.Initialize(mainVm);

        // App startet mit dem Dashboard
        MainPage = new NavigationPage(services.GetRequiredService<DashboardPage>())
        {
            BarBackgroundColor = Color.FromArgb("#2C1A0E"),
            BarTextColor = Color.FromArgb("#C8A96E")
        };
    }

    /// <summary>Wechselt zur Charakteransicht (TabBar-Shell).</summary>
    public void SwitchToCharacterShell(IServiceProvider services)
    {
        MainPage = services.GetRequiredService<AppShell>();
    }

    /// <summary>Wechselt zurück zum Dashboard (z. B. aus Einstellungen).</summary>
    public void SwitchToDashboard(IServiceProvider services)
    {
        MainPage = new NavigationPage(services.GetRequiredService<DashboardPage>())
        {
            BarBackgroundColor = Color.FromArgb("#2C1A0E"),
            BarTextColor = Color.FromArgb("#C8A96E")
        };
    }
}
