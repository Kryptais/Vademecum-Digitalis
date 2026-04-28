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
        string phase = "derived_values",
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
            .ThenBy(candidate => candidate.Effect.Priority)
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
                    : effect.Title
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
        string phase,
        IReadOnlySet<string> activeConditions)
    {
        if (!effect.IsMechanical ||
            effect.Operation is null ||
            effect.Value is null ||
            !string.Equals(effect.Target, target, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(effect.Phase, phase, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return effect.RequiredConditions.All(activeConditions.Contains);
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
            if (group.Key.Stacking == RuleEffectStacking.Highest)
            {
                yield return group
                    .OrderByDescending(candidate => Math.Abs(GetAppliedValue(candidate.Effect, candidate.Source.Level)))
                    .ThenBy(candidate => candidate.Source.SourceId, StringComparer.Ordinal)
                    .ThenBy(candidate => candidate.Effect.Id, StringComparer.Ordinal)
                    .First();
                continue;
            }

            if (group.Key.Stacking == RuleEffectStacking.Replace)
            {
                yield return group
                    .OrderByDescending(candidate => candidate.Effect.Priority)
                    .ThenByDescending(candidate => candidate.Source.SourceId, StringComparer.Ordinal)
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

    private static decimal ApplyOperation(decimal current, RuleModifierOperation operation, decimal value)
    {
        return operation switch
        {
            RuleModifierOperation.Add => current + value,
            RuleModifierOperation.Multiply => current * value,
            RuleModifierOperation.MinCap => Math.Max(current, value),
            RuleModifierOperation.MaxCap => Math.Min(current, value),
            RuleModifierOperation.Override => value,
            _ => current
        };
    }

    private static int OperationOrder(RuleModifierOperation operation) => operation switch
    {
        RuleModifierOperation.Add => 0,
        RuleModifierOperation.Multiply => 1,
        RuleModifierOperation.MinCap => 2,
        RuleModifierOperation.MaxCap => 2,
        RuleModifierOperation.Override => 3,
        _ => 99
    };

    private sealed record EffectCandidate(RuleEffectSource Source, RuleEffect Effect);
}

