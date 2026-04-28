using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.Services.RuleEngine;

/// <summary>
/// Wendet mechanische Regeleffekte deterministisch auf einen Zielwert an
/// und erzeugt ein Audit-Log für die spätere UI-Erklärung.
/// </summary>
public sealed class EffectResolver
{
    public RuleEffectResolution Resolve(
        string target,
        decimal baseValue,
        IEnumerable<RuleEffectSource> sources,
        ModifierPhase phase = ModifierPhase.DerivedValues,
        IEnumerable<string>? activeConditions = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentNullException.ThrowIfNull(sources);

        var activeConditionSet = new HashSet<string>(
            activeConditions ?? [],
            StringComparer.OrdinalIgnoreCase);

        var candidates = sources
            .SelectMany(source => source.Effects.Select(effect => new EffectCandidate(source, effect)))
            .Where(candidate => IsApplicable(candidate.Effect, target, phase, activeConditionSet))
            .ToList();

        var ordered = ApplyStacking(candidates)
            .OrderBy(candidate => OperationOrder(candidate.Effect.Operation!.Value))
            .ThenBy(candidate => candidate.Source.SourceId, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Effect.Id, StringComparer.Ordinal)
            .ToList();

        var current = baseValue;
        var audit = new List<RuleEffectAuditEntry>();

        foreach (var candidate in ordered)
        {
            var effect = candidate.Effect;
            var operation = effect.Operation!.Value;
            var appliedValue = GetAppliedValue(effect, candidate.Source.Level);
            var before = current;
            current = ApplyOperation(current, operation, appliedValue);

            audit.Add(new RuleEffectAuditEntry
            {
                Target = target,
                SourceId = candidate.Source.SourceId,
                SourceName = candidate.Source.SourceName,
                EffectId = effect.Id,
                Operation = operation,
                AppliedValue = appliedValue,
                Before = before,
                After = current,
                Reason = effect.PerLevel
                    ? $"{effect.Value} x Stufe {Math.Max(1, candidate.Source.Level)}"
                    : (effect.Title ?? effect.Id)
            });
        }

        return new RuleEffectResolution
        {
            Target = target,
            BaseValue = baseValue,
            FinalValue = current,
            AuditEntries = audit
        };
    }

    private static bool IsApplicable(
        RuleEffect effect,
        string target,
        ModifierPhase phase,
        IReadOnlySet<string> activeConditions)
    {
        if (effect.Kind != EffectKind.Modifier ||
            effect.Operation is null ||
            effect.Value is null ||
            !string.Equals(effect.Target, target, StringComparison.OrdinalIgnoreCase) ||
            effect.Phase != phase)
        {
            return false;
        }

        var required = effect.Condition?.All.Select(c => c.Fact) ?? [];
        return required.All(activeConditions.Contains);
    }

    private static IEnumerable<EffectCandidate> ApplyStacking(IEnumerable<EffectCandidate> candidates)
    {
        foreach (var group in candidates.GroupBy(candidate => new
                 {
                     candidate.Effect.Target,
                     candidate.Effect.Operation,
                     candidate.Effect.Stacking
                 }))
        {
            if (group.Key.Stacking == StackingRule.Highest)
            {
                yield return group
                    .OrderByDescending(candidate => Math.Abs(GetAppliedValue(candidate.Effect, candidate.Source.Level)))
                    .ThenBy(candidate => candidate.Source.SourceId, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Effect.Id, StringComparer.Ordinal)
                    .First();
                continue;
            }

            if (group.Key.Stacking == StackingRule.Replace)
            {
                yield return group
                    .OrderByDescending(candidate => candidate.Source.SourceId, StringComparer.Ordinal)
                    .ThenByDescending(candidate => candidate.Effect.Id, StringComparer.Ordinal)
                    .First();
                continue;
            }

            foreach (var candidate in group)
                yield return candidate;
        }
    }

    private static decimal GetAppliedValue(RuleEffect effect, int sourceLevel)
    {
        var value = effect.Value ?? 0;
        return effect.PerLevel ? value * Math.Max(1, sourceLevel) : value;
    }

    private static decimal ApplyOperation(decimal current, ModifierOp operation, decimal value)
    {
        return operation switch
        {
            ModifierOp.Add => current + value,
            ModifierOp.Multiply => current * value,
            ModifierOp.MinCap => Math.Max(current, value),
            ModifierOp.MaxCap => Math.Min(current, value),
            ModifierOp.Override => value,
            _ => current
        };
    }

    private static int OperationOrder(ModifierOp operation) => operation switch
    {
        ModifierOp.Add => 0,
        ModifierOp.Multiply => 1,
        ModifierOp.MinCap => 2,
        ModifierOp.MaxCap => 2,
        ModifierOp.Override => 3,
        _ => 99
    };

    private sealed record EffectCandidate(RuleEffectSource Source, RuleEffect Effect);
}
