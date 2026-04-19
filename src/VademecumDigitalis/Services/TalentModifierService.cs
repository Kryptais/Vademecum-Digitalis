using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>
/// Berechnet effektive Probenwerte für Talente unter Berücksichtigung aller aktiven
/// Modifikatoren aus Vorteilen, Nachteilen und Sonderfertigkeiten.
/// </summary>
public class TalentModifierService
{
    private readonly VorteilNachteilService _vnService;
    private readonly SpecialAbilityService _sfService;

    public TalentModifierService(VorteilNachteilService vnService, SpecialAbilityService sfService)
    {
        ArgumentNullException.ThrowIfNull(vnService);
        ArgumentNullException.ThrowIfNull(sfService);
        _vnService = vnService;
        _sfService = sfService;
    }

    /// <summary>
    /// Aktualisiert alle berechneten Probenwerte und Begabungs-Flags für die Talente.
    /// </summary>
    public void UpdateTalentProben(
        IReadOnlyList<TalentGroup> gruppen,
        IReadOnlyDictionary<string, int> attribute,
        IReadOnlyList<CharakterVorteilNachteilEintrag> vnEintraege,
        IReadOnlyList<CharakterSonderfertigkeitEintrag> sfEintraege)
    {
        var modifiers = CollectActiveModifiers(vnEintraege, sfEintraege);

        foreach (var gruppe in gruppen)
        {
            foreach (var row in gruppe.Eintraege)
            {
                UpdateRow(row, gruppe.Gruppe, attribute, modifiers, vnEintraege);
            }
        }
    }

    private void UpdateRow(
        TalentRow row,
        string gruppenName,
        IReadOnlyDictionary<string, int> attribute,
        List<ActiveModifier> modifiers,
        IReadOnlyList<CharakterVorteilNachteilEintrag> vnEintraege)
    {
        // Begabung: Vorteil "Begabung" mit Notiz == Talentname
        row.HatBegabung = vnEintraege.Any(vn =>
            string.Equals(vn.VnId, "begabung", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(vn.Notiz?.Trim(), row.Talent, StringComparison.OrdinalIgnoreCase));

        // Begabung gibt +1 auf alle drei Proben des Talents
        int begabungBonus = row.HatBegabung ? 1 : 0;

        row.ProbeWert1 = GetEffectiveValue(row.Talent, gruppenName, row.Probe1, attribute, modifiers) + begabungBonus;
        row.ProbeWert2 = GetEffectiveValue(row.Talent, gruppenName, row.Probe2, attribute, modifiers) + begabungBonus;
        row.ProbeWert3 = GetEffectiveValue(row.Talent, gruppenName, row.Probe3, attribute, modifiers) + begabungBonus;

        // FW-Bonus: aktuell keine Standard-FW-Boni
        row.FwBonus = 0;
    }

    private static int GetEffectiveValue(
        string talentName,
        string gruppenName,
        string probeAttribut,
        IReadOnlyDictionary<string, int> attribute,
        List<ActiveModifier> modifiers)
    {
        if (!attribute.TryGetValue(probeAttribut, out var basisWert))
            return 0;

        var bonus = 0;
        foreach (var mod in modifiers)
        {
            bonus += mod.Mod.Typ switch
            {
                ModifikatorTyp.Attribut when string.Equals(mod.Mod.Ziel, probeAttribut, StringComparison.OrdinalIgnoreCase)
                    => mod.Mod.Wert * mod.Stufe,
                ModifikatorTyp.Talent when string.Equals(mod.Mod.Ziel, talentName, StringComparison.OrdinalIgnoreCase)
                    => mod.Mod.Wert * mod.Stufe,
                ModifikatorTyp.TalentGruppe when string.Equals(mod.Mod.Ziel, gruppenName, StringComparison.OrdinalIgnoreCase)
                    => mod.Mod.Wert * mod.Stufe,
                _ => 0
            };
        }

        return basisWert + bonus;
    }

    private List<ActiveModifier> CollectActiveModifiers(
        IReadOnlyList<CharakterVorteilNachteilEintrag> vnEintraege,
        IReadOnlyList<CharakterSonderfertigkeitEintrag> sfEintraege)
    {
        var result = new List<ActiveModifier>();

        foreach (var vn in vnEintraege)
        {
            var katalog = _vnService.Catalog.FirstOrDefault(k =>
                string.Equals(k.Id, vn.VnId, StringComparison.OrdinalIgnoreCase));
            if (katalog?.ProbenModifikatoren is { Count: > 0 } mods)
            {
                foreach (var mod in mods)
                {
                    result.Add(new ActiveModifier(mod, vn.Stufe));
                }
            }
        }

        foreach (var sf in sfEintraege)
        {
            var katalog = _sfService.Catalog.FirstOrDefault(k =>
                string.Equals(k.Id, sf.SfId, StringComparison.OrdinalIgnoreCase));
            if (katalog?.ProbenModifikatoren is { Count: > 0 } mods)
            {
                foreach (var mod in mods)
                {
                    result.Add(new ActiveModifier(mod, sf.Stufe));
                }
            }
        }

        return result;
    }

    private readonly record struct ActiveModifier(ProbenModifikator Mod, int Stufe);
}
