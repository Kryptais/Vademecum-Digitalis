namespace VademecumDigitalis.Models;

public class CharacterSheet
{
    // Allgemeine Charakterdaten (DSA 5 Hauptblatt)
    public string Name { get; set; } = string.Empty;
    public string Spieler { get; set; } = string.Empty;
    public string Spezies { get; set; } = string.Empty;
    public string Kultur { get; set; } = string.Empty;
    public string Profession { get; set; } = string.Empty;
    public string Geschlecht { get; set; } = string.Empty;
    public string Geburtstag { get; set; } = string.Empty;
    public string Alter { get; set; } = string.Empty;
    public string Größe { get; set; } = string.Empty;
    public string Gewicht { get; set; } = string.Empty;
    public string Haarfarbe { get; set; } = string.Empty;
    public string Augenfarbe { get; set; } = string.Empty;
    public string Sozialstatus { get; set; } = string.Empty;

    // DSA 5 Hauptattribute/Eigenschaften
    public int Mut { get; set; } = 8;
    public int Klugheit { get; set; } = 8;
    public int Intuition { get; set; } = 8;
    public int Charisma { get; set; } = 8;
    public int Fingerfertigkeit { get; set; } = 8;
    public int Gewandtheit { get; set; } = 8;
    public int Konstitution { get; set; } = 8;
    public int Körperkraft { get; set; } = 8;

    // Basiswerte – zugekaufte Modifikatoren (über AP-Käufe)
    public int LebensenergieZugekauft { get; set; } = 0;
    public int AstralenergieZugekauft { get; set; } = 0;
    public int KarmaenergieZugekauft { get; set; } = 0;
    public int SeelenkraftZugekauft { get; set; } = 0;
    public int ZähigkeitZugekauft { get; set; } = 0;

    // Basiswerte – Boni aus Vorteilen / Sonderfertigkeiten etc.
    public int LebensenergieVorteilsBonus { get; set; } = 0;
    public int AstralenergieVorteilsBonus { get; set; } = 0;
    public int KarmaenergieVorteilsBonus { get; set; } = 0;
    public int SeelenkraftVorteilsBonus { get; set; } = 0;
    public int ZähigkeitVorteilsBonus { get; set; } = 0;
    public int InitiativeBasisVorteilsBonus { get; set; } = 0;
    public int GeschwindigkeitVorteilsBonus { get; set; } = 0;

    // Legacy-Felder für Abwärtskompatibilität (werden beim Laden migriert)
    public int Lebensenergie { get; set; } = 0;
    public int Astralenergie { get; set; } = 0;
    public int Karmaenergie { get; set; } = 0;
    public int Seelenkraft { get; set; } = 0;
    public int Zähigkeit { get; set; } = 0;
    public int InitiativeBasis { get; set; } = 0;
    public int Geschwindigkeit { get; set; } = 8;

    // V1-Felder laut Anforderungen
    public int AbenteuerpunkteGesamt { get; set; } = 1100;
    public int AbenteuerpunkteVerfuegbar { get; set; } = 0;
    public int AbenteuerpunkteAusgegeben { get; set; } = 1100;

    public int SchicksalspunkteGesamt { get; set; } = 3;
    public int SchicksalspunkteVerfuegbar { get; set; } = 3;

    // Freitextlisten für V1
    public string Vorteile { get; set; } = string.Empty;
    public string Nachteile { get; set; } = string.Empty;
    public string Talente { get; set; } = string.Empty;
    public string Kampftalente { get; set; } = string.Empty;

    /// <summary>Aktuelle Lebensenergie (für Kampf-Tab Schmerzstufen-Tracking).</summary>
    public int AktuelleLebensenergie { get; set; } = -1;

    /// <summary>Kampf-Status (Betäubung, Verwirrung, etc.) - Dictionary: StatusName -> Stufe (0-4).</summary>
    public Dictionary<string, int> KampfStati { get; set; } = new();

    // Aktuelles aventurisches Datum (für Altersberechnung)
    public string AktuellesDatumStr { get; set; } = string.Empty;

    // Charakter-Ereignisse
    public List<CharakterEreignis> Ereignisse { get; set; } = [];

    // Sonderfertigkeiten
    public List<CharakterSonderfertigkeitEintrag> SonderfertigkeitListe { get; set; } = [];

    // Vorteile / Nachteile
    public List<CharakterVorteilNachteilEintrag> VorteilNachteilListe { get; set; } = [];
}
