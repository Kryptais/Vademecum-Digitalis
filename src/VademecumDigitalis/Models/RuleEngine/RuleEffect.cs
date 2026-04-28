using System.Text.Json.Serialization;

namespace VademecumDigitalis.Models.RuleEngine;

/// <summary>Trennt reine Beschreibungseffekte von mechanischen Wertänderungen.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleEffectKind
{
    Narrative,
    Modifier
}

/// <summary>Operation eines mechanischen Effekts in der Berechnungspipeline.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleModifierOperation
{
    Add,
    Multiply,
    Override,
    MinCap,
    MaxCap
}

/// <summary>Stacking-Regel für mehrere Effekte auf dasselbe Ziel.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleEffectStacking
{
    Stack,
    Highest,
    Replace
}

/// <summary>Ein Effekt aus Vorteil, Nachteil, Sonderfertigkeit oder Homebrew-Regel.</summary>
public sealed record RuleEffect
{
    public string Id { get; init; } = string.Empty;
    public RuleEffectKind Kind { get; init; }

    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<string> Tags { get; init; } = [];

    public string Target { get; init; } = string.Empty;
    public RuleModifierOperation? Operation { get; init; }
    public decimal? Value { get; init; }
    public bool PerLevel { get; init; }
    public string Phase { get; init; } = "derived_values";
    public RuleEffectStacking Stacking { get; init; } = RuleEffectStacking.Stack;
    public int Priority { get; init; }

    /// <summary>Alle Condition-Keys müssen aktiv sein, damit der Effekt gilt.</summary>
    public List<string> RequiredConditions { get; init; } = [];

    [JsonIgnore]
    public bool IsMechanical => Kind == RuleEffectKind.Modifier;
}

