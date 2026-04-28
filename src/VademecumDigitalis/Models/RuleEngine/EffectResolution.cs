namespace VademecumDigitalis.Models.RuleEngine;

/// <summary>Aktive Regelquelle, aus der Effekte für eine Berechnung stammen.</summary>
public sealed record RuleEffectSource
{
    public string SourceId { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public int Level { get; init; } = 1;
    public IReadOnlyList<RuleEffect> Effects { get; init; } = [];
}

/// <summary>Ein auditierbarer Rechenschritt eines mechanischen Effekts.</summary>
public sealed record RuleEffectAuditEntry
{
    public string Target { get; init; } = string.Empty;
    public string SourceId { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string EffectId { get; init; } = string.Empty;
    public RuleModifierOperation Operation { get; init; }
    public decimal AppliedValue { get; init; }
    public decimal Before { get; init; }
    public decimal After { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>Ergebnis einer Wertberechnung inklusive Herkunftsnachweis.</summary>
public sealed record RuleEffectResolution
{
    public string Target { get; init; } = string.Empty;
    public decimal BaseValue { get; init; }
    public decimal FinalValue { get; init; }
    public IReadOnlyList<RuleEffectAuditEntry> AuditEntries { get; init; } = [];
}

