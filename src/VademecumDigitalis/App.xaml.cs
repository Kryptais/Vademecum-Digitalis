using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class App : Application
{
    public App(MainPageViewModel mainVm)
    {
        InitializeComponent();

        // Initialisiere die zentrale Session, damit alle Pages dasselbe ViewModel nutzen
        CharacterSheetSession.Initialize(mainVm);

        // Lade gespeicherte Daten beim App-Start
        MainThread.BeginInvokeOnMainThread(async () => await mainVm.LoadDataAsync());

        MainPage = new AppShell();
    }
}
