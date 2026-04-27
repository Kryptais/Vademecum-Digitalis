using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class SonderfertigkeitenPage : ContentPage
{
    public SonderfertigkeitenPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }

    private async void OnEintragHinzufuegen(object sender, EventArgs e)
    {
        var vm = CharacterSheetSession.Current;

        string? name = await DisplayPromptAsync(
            "Sonderfertigkeit hinzufügen",
            "Name der Sonderfertigkeit:",
            placeholder: "z. B. Klingentänzer");
        if (string.IsNullOrWhiteSpace(name)) return;

        var kategorieNamen = new[] { "Allgemein", "Kampf", "Magisch", "Karmal", "Sprache/Schrift" };
        string? kategorieWahl = await DisplayActionSheet("Kategorie wählen", "Abbrechen", null, kategorieNamen);
        if (kategorieWahl == null || kategorieWahl == "Abbrechen") return;

        var kategorie = kategorieWahl switch
        {
            "Kampf" => SonderfertigkeitKategorie.Kampf,
            "Magisch" => SonderfertigkeitKategorie.Magisch,
            "Karmal" => SonderfertigkeitKategorie.Karmal,
            "Sprache/Schrift" => SonderfertigkeitKategorie.Sprachschrift,
            _ => SonderfertigkeitKategorie.Allgemein
        };

        string? notiz = await DisplayPromptAsync(
            "Notiz (optional)",
            "Kurzbeschreibung oder Stufe:",
            placeholder: "z. B. Stufe I");

        var eintrag = new CharakterSonderfertigkeitEintrag
        {
            Name = name.Trim(),
            Kategorie = kategorie,
            Notiz = notiz?.Trim() ?? string.Empty
        };

        vm.SonderfertigkeitHinzufuegen(eintrag);
    }

    private void OnEintragLoeschen(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharakterSonderfertigkeitEintrag eintrag)
        {
            CharacterSheetSession.Current.SonderfertigkeitEntfernen(eintrag);
        }
    }
}
