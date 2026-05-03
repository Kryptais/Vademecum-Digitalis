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
        // Begabung: Vorteil "begabung" mit Notiz == Talentname
        row.HatBegabung = vnEintraege.Any(vn =>
            string.Equals(vn.VnId, "begabung", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(vn.Notiz?.Trim(), row.Talent, StringComparison.OrdinalIgnoreCase));

        // Unfähigkeit: Nachteil "unfaehig" mit Notiz == Talentname
        row.HatUnfaehigkeit = vnEintraege.Any(vn =>
            string.Equals(vn.VnId, "unfaehig", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(vn.Notiz?.Trim(), row.Talent, StringComparison.OrdinalIgnoreCase));

        int begabungBonus = row.HatBegabung ? 1 : 0;

        row.ProbeWert1 = GetEffectiveValue(row.Talent, gruppenName, row.Probe1, attribute, modifiers) + begabungBonus;
        row.ProbeWert2 = GetEffectiveValue(row.Talent, gruppenName, row.Probe2, attribute, modifiers) + begabungBonus;
        row.ProbeWert3 = GetEffectiveValue(row.Talent, gruppenName, row.Probe3, attribute, modifiers) + begabungBonus;

        // FW-Bonus aus ExplicitEffects (z.B. "fw.fliegen" +2)
        int fwBonus = 0;
        foreach (var vnEintrag in vnEintraege)
        {
            var katalog = _vnService.Catalog.FirstOrDefault(k =>
                string.Equals(k.Id, vnEintrag.VnId, StringComparison.OrdinalIgnoreCase));
            if (katalog?.ExplicitEffects is { Count: > 0 } effects)
            {
                foreach (var effect in effects)
                {
                    if (effect.Target?.StartsWith("fw.", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var targetTalent = effect.Target["fw.".Length..];
                        if (string.Equals(targetTalent, row.Talent, StringComparison.OrdinalIgnoreCase))
                        {
                            fwBonus += (int)(effect.Value ?? 0);
                        }
                    }
                }
            }
        }
        row.FwBonus = fwBonus;

        // BonusInfo: zeige welche VN/SF dieses Talent beeinflussen
        row.BonusInfo = BuildBonusInfo(row.Talent, gruppenName, modifiers, row.HatBegabung, row.HatUnfaehigkeit, fwBonus);
    }

    private static string BuildBonusInfo(
        string talentName,
        string gruppenName,
        List<ActiveModifier> modifiers,
        bool hatBegabung,
        bool hatUnfaehigkeit = false,
        int fwBonus = 0)
    {
        var parts = new List<string>();

        if (hatBegabung)
            parts.Add("Begabung: Proben +1");

        if (hatUnfaehigkeit)
            parts.Add("Unfähigkeit: besten Wurf neu würfeln");

        if (fwBonus != 0)
            parts.Add($"FW {(fwBonus > 0 ? "+" : "")}{fwBonus}");

        foreach (var mod in modifiers)
        {
            bool betrifftTalent = mod.Mod.Typ switch
            {
                ModifikatorTyp.Talent      => string.Equals(mod.Mod.Ziel, talentName,  StringComparison.OrdinalIgnoreCase),
                ModifikatorTyp.TalentGruppe => string.Equals(mod.Mod.Ziel, gruppenName, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
            if (betrifftTalent)
            {
                var wert = mod.Mod.Wert * mod.Stufe;
                var prefix = wert >= 0 ? "+" : string.Empty;
                parts.Add($"{mod.Mod.Ziel} {prefix}{wert}");
            }
        }

        return string.Join(", ", parts);
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
