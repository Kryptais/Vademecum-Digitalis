using VademecumDigitalis.Models;
using VademecumDigitalis.Services;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class AdvantagesPage : ContentPage
{
    public AdvantagesPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }

    private async void OnEintragHinzufuegen(object sender, EventArgs e)
    {
        var vm = CharacterSheetSession.Current;

        // Alle Vorteile und Nachteile als Auswahl aufbereiten
        var katalogNamen = VorteilNachteilKatalog.Alle
            .Select(v => $"{v.Name} ({(v.Typ == VorteilNachteilTyp.Vorteil ? "V" : "N")}, {v.ApKostenProStufe:+#;-#;0} AP/Stufe)")
            .ToArray();

        string? auswahl = await DisplayActionSheet("Vorteil/Nachteil auswählen", "Abbrechen", null, katalogNamen);
        if (auswahl == null || auswahl == "Abbrechen") return;

        // Katalog-Eintrag anhand des Namens (ohne Klammerzusatz) finden
        string name = auswahl.Split('(')[0].Trim();
        var katalogEintrag = VorteilNachteilService.FindByName(name);
        if (katalogEintrag == null) return;

        int stufe = 1;
        if (katalogEintrag.MaxStufe > 1)
        {
            var stufenOptionen = Enumerable.Range(1, katalogEintrag.MaxStufe)
                .Select(s => s.ToString())
                .ToArray();
            string? stufenWahl = await DisplayActionSheet($"Stufe für '{katalogEintrag.Name}'", "Abbrechen", null, stufenOptionen);
            if (stufenWahl == null || stufenWahl == "Abbrechen") return;
            stufe = int.Parse(stufenWahl);
        }

        var eintrag = VorteilNachteilService.EintragErstellen(katalogEintrag, stufe);
        vm.VorteilNachteilHinzufuegen(eintrag);
    }

    private void OnEintragLoeschen(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CharaktervorteilEintrag eintrag)
        {
            CharacterSheetSession.Current.VorteilNachteilEntfernen(eintrag);
        }
    }
}
