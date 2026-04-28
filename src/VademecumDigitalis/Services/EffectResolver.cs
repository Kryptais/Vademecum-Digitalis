using VademecumDigitalis.Models;
using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.Services;

/// <summary>
/// Ein Schritt im Audit-Log – zeigt warum sich ein Wert verändert hat.
/// </summary>
public sealed class AuditStep
{
    public string Source { get; init; } = string.Empty;   // z. B. "flink"
    public string EffectId { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;
    public decimal Before { get; init; }
    public decimal After { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Ergebnis der Berechnung für einen einzelnen Zielwert.
/// </summary>
public sealed class ResolvedValue
{
    public string Target { get; init; } = string.Empty;
    public decimal FinalValue { get; init; }
    public IReadOnlyList<AuditStep> AuditLog { get; init; } = [];

    /// <summary>Lesbare Zusammenfassung aller Quellen, z. B. für Tooltips.</summary>
    public string AuditSummary =>
        AuditLog.Count == 0
            ? string.Empty
            : string.Join("\n", AuditLog.Select(s =>
                $"{s.Source}: {(s.After - s.Before >= 0 ? "+" : "")}{s.After - s.Before:G} → {s.After:G}"));
}

/// <summary>
/// Berechnet Endwerte aus einer Basis + aktiven RuleEffects.
/// Pipeline: Add → Multiply → MinCap/MaxCap → Override (je Phase, deterministisch).
/// </summary>
public sealed class EffectResolver
{
    /// <summary>
    /// Wendet alle aktiven Effekte einer Phase auf einen Startwert an.
    /// </summary>
    /// <param name="target">Zielwert-Schlüssel, z. B. "derived.GS".</param>
    /// <param name="baseValue">Ausgangswert (aus Spezies, Ausrüstung o. ä.).</param>
    /// <param name="activeSources">
    ///   Alle aktiven Regel-Quellen als (sourceName, stufe, effects)-Tupel.
    ///   sourceName erscheint im Audit-Log.
    /// </param>
    /// <param name="phase">Nur Effekte dieser Phase werden berücksichtigt.</param>
    /// <param name="activeConditionKeys">Aktive Condition-Keys aus dem RuleContext.</param>
    public ResolvedValue Resolve(
        string target,
        decimal baseValue,
        IEnumerable<(string SourceName, int Stufe, IEnumerable<RuleEffect> Effects)> activeSources,
        ModifierPhase phase,
        IReadOnlySet<string>? activeConditionKeys = null)
    {
        var audit = new List<AuditStep>();
        var current = baseValue;

        var relevantEffects = activeSources
            .SelectMany(s => s.Effects.Select(e => (s.SourceName, s.Stufe, Effect: e)))
            .Where(x =>
                x.Effect.Kind == EffectKind.Modifier &&
                x.Effect.Target == target &&
                x.Effect.Phase == phase &&
                ConditionMet(x.Effect.Condition, activeConditionKeys))
            .ToList();

        current = ApplyOp(current, ModifierOp.Add, relevantEffects, StackingRule.Stack, audit);
        current = ApplyOp(current, ModifierOp.Multiply, relevantEffects, StackingRule.Stack, audit);
        current = ApplyOp(current, ModifierOp.MinCap, relevantEffects, StackingRule.Highest, audit);
        current = ApplyOp(current, ModifierOp.MaxCap, relevantEffects, StackingRule.Highest, audit);
        current = ApplyOp(current, ModifierOp.Override, relevantEffects, StackingRule.Replace, audit);

        return new ResolvedValue
        {
            Target = target,
            FinalValue = current,
            AuditLog = audit
        };
    }

    // ---------------------------------------------------------------------------

    private static decimal ApplyOp(
        decimal current,
        ModifierOp op,
        IEnumerable<(string SourceName, int Stufe, RuleEffect Effect)> all,
        StackingRule defaultStacking,
        List<AuditStep> audit)
    {
        var matching = all
            .Where(x => x.Effect.Operation == op)
            .ToList();

        if (matching.Count == 0)
            return current;

        // Stacking-Auflösung: Verwende Effekt-eigene Regel, falls angegeben.
        var stacking = matching[0].Effect.Stacking;

        decimal delta = op switch
        {
            ModifierOp.Add => ComputeStackedAdd(matching, stacking),
            ModifierOp.Multiply => ComputeStackedMultiply(matching, stacking),
            ModifierOp.MinCap => ComputeMin(matching, stacking),
            ModifierOp.MaxCap => ComputeMax(matching, stacking),
            ModifierOp.Override => ComputeOverride(matching, stacking),
            _ => 0
        };

        decimal after = op switch
        {
            ModifierOp.Add => current + delta,
            ModifierOp.Multiply => current * delta,
            ModifierOp.MinCap => Math.Max(current, delta),
            ModifierOp.MaxCap => Math.Min(current, delta),
            ModifierOp.Override => delta,
            _ => current
        };

        if (after != current)
        {
            var sources = string.Join(", ", matching.Select(x => x.SourceName).Distinct());
            audit.Add(new AuditStep
            {
                Source = sources,
                EffectId = matching[0].Effect.Id,
                Target = matching[0].Effect.Target ?? string.Empty,
                Before = current,
                After = after,
                Reason = BuildReason(op, matching)
            });
        }

        return after;
    }

    private static decimal ComputeStackedAdd(
        IEnumerable<(string, int Stufe, RuleEffect Effect)> items, StackingRule stacking)
    {
        var values = items.Select(x => EffectValue(x.Effect, x.Stufe));
        return stacking == StackingRule.Highest ? values.Max() : values.Sum();
    }

    private static decimal ComputeStackedMultiply(
        IEnumerable<(string, int Stufe, RuleEffect Effect)> items, StackingRule stacking)
    {
        var values = items.Select(x => EffectValue(x.Effect, x.Stufe));
        return stacking == StackingRule.Highest ? values.Max() : values.Aggregate(1m, (a, v) => a * v);
    }

    private static decimal ComputeMin(
        IEnumerable<(string, int Stufe, RuleEffect Effect)> items, StackingRule _)
        => items.Select(x => EffectValue(x.Effect, x.Stufe)).Max();

    private static decimal ComputeMax(
        IEnumerable<(string, int Stufe, RuleEffect Effect)> items, StackingRule _)
        => items.Select(x => EffectValue(x.Effect, x.Stufe)).Min();

    private static decimal ComputeOverride(
        IEnumerable<(string, int Stufe, RuleEffect Effect)> items, StackingRule _)
        => items.Select(x => EffectValue(x.Effect, x.Stufe)).Last();

    private static decimal EffectValue(RuleEffect e, int stufe)
    {
        var v = e.Value ?? 0m;
        return e.PerLevel ? v * stufe : v;
    }

    private static bool ConditionMet(ConditionGroup? condition, IReadOnlySet<string>? activeKeys)
    {
        if (condition is null)
            return true;

        if (activeKeys is null)
            return condition.All.Count == 0 && condition.Any.Count == 0;

        bool allMet = condition.All.Count == 0 || condition.All.All(f => EvalFact(f, activeKeys));
        bool anyMet = condition.Any.Count == 0 || condition.Any.Any(f => EvalFact(f, activeKeys));

        return allMet && anyMet;
    }

    private static bool EvalFact(ConditionFact fact, IReadOnlySet<string> activeKeys)
    {
        // Einfachste Auswertung: key muss aktiv sein (Eq true) oder inaktiv (NotEq true).
        bool isActive = activeKeys.Contains(fact.Fact);
        return fact.Op switch
        {
            ConditionOp.Eq => isActive == (fact.Value is bool b ? b : true),
            ConditionOp.NotEq => isActive != (fact.Value is bool b2 ? b2 : true),
            _ => true   // Numerische Ops für Keys noch nicht relevant; als erfüllt werten.
        };
    }

    private static string BuildReason(
        ModifierOp op,
        IEnumerable<(string SourceName, int Stufe, RuleEffect Effect)> items)
    {
        var parts = items.Select(x =>
        {
            var v = EffectValue(x.Effect, x.Stufe);
            var suffix = x.Effect.PerLevel ? $" (Stufe {x.Stufe})" : string.Empty;
            return $"{x.SourceName}{suffix}: {op} {v:G}";
        });
        return string.Join("; ", parts);
    }
}
