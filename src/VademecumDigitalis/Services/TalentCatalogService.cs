using System.Text.Json;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

/// <summary>Eintrag im Talent-Katalog (Spielweltdaten, keine Charakterwerte).</summary>
public sealed record TalentKatalogEintrag(
    string Name,
    string Gruppe,
    string Sf,
    string Probe1,
    string Probe2,
    string Probe3,
    string Be
);

/// <summary>Lädt den DSA-5-Talent-Katalog aus der eingebetteten talente.json.</summary>
public class TalentCatalogService
{
    private List<TalentKatalogEintrag>? _katalog;

    public IReadOnlyList<TalentKatalogEintrag> Katalog => _katalog ?? [];

    public async Task EnsureLoadedAsync()
    {
        if (_katalog is not null) return;
        await using var stream = await FileSystem.OpenAppPackageFileAsync("talente.json");
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        _katalog = Parse(json);
    }

    public IReadOnlyList<TalentGroup> BuildTalentGruppen()
    {
        if (_katalog is null)
            throw new InvalidOperationException("TalentCatalogService: EnsureLoadedAsync() muss zuerst aufgerufen werden.");

        return _katalog
            .GroupBy(e => e.Gruppe, StringComparer.Ordinal)
            .Select(g => new TalentGroup(g.Key,
                g.Select(e => new TalentRow
                {
                    Talent = e.Name,
                    Gruppe = e.Gruppe,
                    Steigerungsfaktor = e.Sf,
                    Probe1 = e.Probe1,
                    Probe2 = e.Probe2,
                    Probe3 = e.Probe3,
                    Belastungseinfluss = e.Be
                })))
            .ToList();
    }

    /// <summary>Sucht einen Katalogeintrag nach exaktem Namen (case-insensitive).</summary>
    public TalentKatalogEintrag? FindByName(string name) =>
        _katalog?.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));

    private static List<TalentKatalogEintrag> Parse(string json)
    {
        var doc = JsonDocument.Parse(json);
        var result = new List<TalentKatalogEintrag>();

        foreach (var gruppe in doc.RootElement.GetProperty("gruppen").EnumerateArray())
        {
            var gruppeName = gruppe.GetProperty("gruppe").GetString() ?? string.Empty;
            foreach (var t in gruppe.GetProperty("talente").EnumerateArray())
            {
                var probe = t.GetProperty("probe").EnumerateArray().Select(p => p.GetString() ?? "").ToArray();
                result.Add(new TalentKatalogEintrag(
                    Name:    t.GetProperty("name").GetString() ?? string.Empty,
                    Gruppe:  gruppeName,
                    Sf:      t.GetProperty("sf").GetString() ?? string.Empty,
                    Probe1:  probe.Length > 0 ? probe[0] : "",
                    Probe2:  probe.Length > 1 ? probe[1] : "",
                    Probe3:  probe.Length > 2 ? probe[2] : "",
                    Be:      t.GetProperty("be").GetString() ?? string.Empty
                ));
            }
        }

        return result;
    }
}
