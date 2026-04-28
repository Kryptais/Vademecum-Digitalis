using System.Text.Json.Serialization;

namespace VademecumDigitalis.Models.RuleEngine;

/// <summary>Trennt reine Beschreibungseffekte von mechanischen Wertänderungen.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectKind
{
    /// <summary>Nur Beschreibungstext, kein Rechenwert (Fluff).</summary>
    Narrative,

    /// <summary>Verändert einen konkreten Zielwert (z. B. derived.GS).</summary>
    Modifier
}

/// <summary>Operation eines mechanischen Effekts in der Berechnungspipeline.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModifierOp
{
    Add,
    Multiply,
    Override,
    MinCap,
    MaxCap
}

/// <summary>Alias für ModifierOp – für Kompatibilität mit dem RuleEngine-Namespace.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RuleModifierOperation { Add, Multiply, Override, MinCap, MaxCap }

/// <summary>
/// Wie sich gleichartige Effekte auf denselben Zielwert verhalten.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum StackingRule
{
    /// <summary>Alle Effekte addieren sich.</summary>
    Stack,

    /// <summary>Nur der stärkste Effekt zählt.</summary>
    Highest,

    /// <summary>Ersetzt den offiziellen Wert vollständig (Homebrew/Override).</summary>
    Replace
}

/// <summary>
/// Berechnungsphase, in der ein Modifier angewendet wird.
/// Reihenfolge: Base → DerivedValues → Checks.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ModifierPhase
{
    Base,
    DerivedValues,
    Checks
}

// ---------------------------------------------------------------------------
// Condition-Ausdrücke
// ---------------------------------------------------------------------------

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConditionOp
{
    Eq,
    NotEq,
    Gte,
    Lte,
    Gt,
    Lt
}

/// <summary>Einzelne Bedingungsaussage, z. B. context.isMounted == true.</summary>
public sealed class ConditionFact
{
    public string Fact { get; init; } = string.Empty;
    public ConditionOp Op { get; init; } = ConditionOp.Eq;

    /// <summary>Vergleichswert als string, bool, int – JSON-konform.</summary>
    public object? Value { get; init; }
}

/// <summary>Gruppiert mehrere Bedingungen (ALL = UND-Verknüpfung).</summary>
public sealed class ConditionGroup
{
    public List<ConditionFact> All { get; init; } = [];
    public List<ConditionFact> Any { get; init; } = [];
}

// ---------------------------------------------------------------------------
// Haupt-Effektklasse
// ---------------------------------------------------------------------------

/// <summary>
/// Ein einzelner Effekt eines Vorteils, Nachteils oder einer Sonderfertigkeit.
/// Entweder narrativ (Fluff) oder ein konkreter Modifier auf einen Zielwert.
/// </summary>
public sealed class RuleEffect
{
    public string Id { get; init; } = string.Empty;
    public EffectKind Kind { get; init; }

    // Narrative ---
    public string? Title { get; init; }
    public string? Description { get; init; }
    public List<string> Tags { get; init; } = [];

    // Modifier ---

    /// <summary>Zielwert im Punkt-Format, z. B. "derived.GS", "attribute.MU", "skill.Kraftakt".</summary>
    public string? Target { get; init; }

    public ModifierOp? Operation { get; init; }

    /// <summary>Basiswert des Modifikators.</summary>
    public decimal? Value { get; init; }

    /// <summary>Wenn true, wird Value mit der aktuellen Stufe des Eintrags multipliziert.</summary>
    public bool PerLevel { get; init; }

    public ModifierPhase Phase { get; init; } = ModifierPhase.DerivedValues;

    public StackingRule Stacking { get; init; } = StackingRule.Stack;

    /// <summary>Optionale Bedingung – Effekt gilt nur wenn alle/any Conditions erfüllt sind.</summary>
    public ConditionGroup? Condition { get; init; }
}

