namespace VademecumDigitalis.Models;

/// <summary>
/// Statischer DSA-5-Katalog aller Vorteile und Nachteile aus dem Grundregelwerk.
/// Werte nach DSA-5-Grundregelwerk (Ulisses Spiele).
/// </summary>
public static class VorteilNachteilKatalog
{
    /// <summary>Alle Vorteile und Nachteile des DSA-5-Grundregelwerks.</summary>
    public static IReadOnlyList<VorteilNachteil> Alle { get; } =
    [
        // ── Vorteile ─────────────────────────────────────────────────────────────

        new VorteilNachteil
        {
            Name = "Begabung",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 4,
            Regeltext = "Für ein gewähltes Talent gilt die Erschwernis durch Behinderung als um 1 reduziert."
        },
        new VorteilNachteil
        {
            Name = "Eisern",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 8,
            Regeltext = "Der Held kann seinen LeP-Wert um bis zu 5 Punkte über 0 sinken lassen, bevor er bewusstlos wird."
        },
        new VorteilNachteil
        {
            Name = "Flink",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 6,
            Regeltext = "GS des Helden erhöht sich um 1."
        },
        new VorteilNachteil
        {
            Name = "Geweiht",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 0,
            Regeltext = "Der Held ist dem Dienst einer Gottheit geweiht und kann Karmaenergie einsetzen."
        },
        new VorteilNachteil
        {
            Name = "Glück I",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 3,
            ApKostenProStufe = 15,
            Regeltext = "Pro Stufe einmal pro Abenteuer einen beliebigen Würfelwurf wiederholen."
        },
        new VorteilNachteil
        {
            Name = "Gut Aussehend",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 1,
            Regeltext = "+1 auf Gesellschaftstalente bei passenden Situationen."
        },
        new VorteilNachteil
        {
            Name = "Hohe Lebenskraft I",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 2,
            ApKostenProStufe = 4,
            Regeltext = "Erhöht den LeP-Wert um 1 pro Stufe."
        },
        new VorteilNachteil
        {
            Name = "Kampfgespür",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 20,
            Regeltext = "+1 auf alle Kampftechnik-Proben."
        },
        new VorteilNachteil
        {
            Name = "Kontakte",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 3,
            Regeltext = "Der Held hat Kontakt zu einer Personengruppe oder Institution."
        },
        new VorteilNachteil
        {
            Name = "Magisch Begabt",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 0,
            Regeltext = "Der Held besitzt magisches Talent und kann AsP einsetzen."
        },
        new VorteilNachteil
        {
            Name = "Richtungssinn",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 5,
            Regeltext = "Der Held weiß immer, wo Norden ist und findet sich in unbekanntem Gelände besser zurecht."
        },
        new VorteilNachteil
        {
            Name = "Schnelle Heilung I",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 3,
            ApKostenProStufe = 4,
            Regeltext = "Pro Stufe +1 auf Regenerationswürfe für Lebensenergie."
        },
        new VorteilNachteil
        {
            Name = "Sprachengenie",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 2,
            Regeltext = "Das Erlernen von Sprachen und Schriften kostet nur halb so viele AP."
        },
        new VorteilNachteil
        {
            Name = "Stabil",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 5,
            Regeltext = "+1 auf Seelenkraft gegen Furcht und Verwirrung."
        },
        new VorteilNachteil
        {
            Name = "Weihegeschenk",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 0,
            Regeltext = "Der Held erhält besondere Fähigkeiten durch sein Weihegeschenk."
        },
        new VorteilNachteil
        {
            Name = "Wohlklang",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 1,
            Regeltext = "Die Stimme des Helden ist besonders angenehm. +1 auf Gesellschaftstalente."
        },
        new VorteilNachteil
        {
            Name = "Zäher Hund I",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 2,
            ApKostenProStufe = 4,
            Regeltext = "Pro Stufe +1 auf Zähigkeit."
        },
        new VorteilNachteil
        {
            Name = "Zweihänder",
            Typ = VorteilNachteilTyp.Vorteil,
            MaxStufe = 1,
            ApKostenProStufe = 8,
            Regeltext = "Keine Abzüge beim Führen von Waffen in beiden Händen."
        },

        // ── Nachteile ────────────────────────────────────────────────────────────

        new VorteilNachteil
        {
            Name = "Blind",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -20,
            Regeltext = "Der Held ist blind. Schwere Abzüge auf alle visuellen Proben."
        },
        new VorteilNachteil
        {
            Name = "Blutrausch",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -20,
            Regeltext = "Im Kampf droht Kontrollverlust bei Verletzungen."
        },
        new VorteilNachteil
        {
            Name = "Goldgier",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 5,
            ApKostenProStufe = -1,
            Regeltext = "Der Held ist geldgierig. Je höher die Stufe, desto stärker der Drang."
        },
        new VorteilNachteil
        {
            Name = "Jähzorn",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 5,
            ApKostenProStufe = -1,
            Regeltext = "Der Held neigt zu unkontrollierten Wutausbrüchen."
        },
        new VorteilNachteil
        {
            Name = "Körperlich Schwach I",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 2,
            ApKostenProStufe = -4,
            Regeltext = "Pro Stufe -1 auf Lebensenergie."
        },
        new VorteilNachteil
        {
            Name = "Langsam",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -6,
            Regeltext = "GS des Helden verringert sich um 1."
        },
        new VorteilNachteil
        {
            Name = "Neugier",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 6,
            ApKostenProStufe = -1,
            Regeltext = "Der Held muss Geheimnissen nachgehen. Je höher die Stufe, desto zwanghafter."
        },
        new VorteilNachteil
        {
            Name = "Pech I",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 3,
            ApKostenProStufe = -10,
            Regeltext = "Pro Stufe einmal pro Abenteuer kann der Meister einen beliebigen Würfelwurf wiederholen lassen."
        },
        new VorteilNachteil
        {
            Name = "Prinzipientreue",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 5,
            ApKostenProStufe = -1,
            Regeltext = "Der Held hält strikt an einem Moralkodex fest."
        },
        new VorteilNachteil
        {
            Name = "Rücksichtslos",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -10,
            Regeltext = "Der Held nimmt keine Rücksicht auf Kollateralschäden."
        },
        new VorteilNachteil
        {
            Name = "Schulden",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -2,
            Regeltext = "Der Held hat Schulden, die er zurückzahlen muss."
        },
        new VorteilNachteil
        {
            Name = "Sucht",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 5,
            ApKostenProStufe = -1,
            Regeltext = "Der Held ist einer Substanz oder Verhaltensweise verfallen."
        },
        new VorteilNachteil
        {
            Name = "Taub",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -10,
            Regeltext = "Der Held ist taub. Abzüge auf alle auditiven Proben."
        },
        new VorteilNachteil
        {
            Name = "Unfähigkeit",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -4,
            Regeltext = "Für ein gewähltes Talent werden alle Proben um 1 erschwert."
        },
        new VorteilNachteil
        {
            Name = "Vorurteile",
            Typ = VorteilNachteilTyp.Nachteil,
            MaxStufe = 1,
            ApKostenProStufe = -4,
            Regeltext = "Der Held hegt starke Vorurteile gegenüber einer Gruppe."
        },
    ];

    /// <summary>Alle Vorteile aus dem Katalog.</summary>
    public static IReadOnlyList<VorteilNachteil> AlleVorteile { get; } =
        Alle.Where(v => v.Typ == VorteilNachteilTyp.Vorteil).ToList();

    /// <summary>Alle Nachteile aus dem Katalog.</summary>
    public static IReadOnlyList<VorteilNachteil> AlleNachteile { get; } =
        Alle.Where(v => v.Typ == VorteilNachteilTyp.Nachteil).ToList();

    /// <summary>Sucht einen Eintrag nach Name (Groß-/Kleinschreibung ignoriert).</summary>
    public static VorteilNachteil? FindByName(string name) =>
        Alle.FirstOrDefault(v => string.Equals(v.Name, name, StringComparison.OrdinalIgnoreCase));
}
