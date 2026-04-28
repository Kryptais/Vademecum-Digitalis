namespace VademecumDigitalis.Models;

/// <summary>
/// Legacy-Fassade für alten statischen VN-Katalog.
/// Der aktive Katalog wird über VorteilNachteilService aus vorteile_nachteile.json geladen.
/// </summary>
[Obsolete("Use VorteilNachteilService.Catalog instead.")]
public static class VorteilNachteilKatalog
{
    public static IReadOnlyList<VorteilNachteil> Alle { get; } = [];
    public static IReadOnlyList<VorteilNachteil> AlleVorteile { get; } = [];
    public static IReadOnlyList<VorteilNachteil> AlleNachteile { get; } = [];

    public static VorteilNachteil? FindByName(string name) => null;
}

