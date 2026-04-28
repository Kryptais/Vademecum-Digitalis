using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.ViewModels.RuleEngine;

/// <summary>
/// Hält auswählbare und berechnete Conditions für die Regelberechnung bereit.
/// Gedacht als bindbare Quelle für ein gemeinsames UC auf Hauptblatt/Talente/Kampf/Zauber.
/// </summary>
public class RuleContextViewModel : INotifyPropertyChanged
{
    public ObservableCollection<RuleContextState> States { get; } = [];

    public IEnumerable<RuleContextState> ManualStates => States.Where(s => s.Source == RuleContextStateSource.Manual);
    public IEnumerable<RuleContextState> DerivedStates => States.Where(s => s.Source == RuleContextStateSource.Derived);

    public RuleContextViewModel()
    {
        SeedDefaults();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void UpsertManualState(string key, string label, bool isActive, string group = "Kontext")
    {
        var existing = States.FirstOrDefault(s => s.Key == key);
        if (existing is null)
        {
            States.Add(new RuleContextState
            {
                Key = key,
                Label = label,
                Group = group,
                Source = RuleContextStateSource.Manual,
                IsSelectable = true,
                IsActive = isActive
            });
        }
        else
        {
            existing.Label = label;
            existing.Group = group;
            existing.Source = RuleContextStateSource.Manual;
            existing.IsSelectable = true;
            existing.IsActive = isActive;
        }

        NotifyDerivedCollections();
    }

    public void UpsertDerivedState(string key, string label, bool isActive, string valueText, string group = "Berechnet")
    {
        var existing = States.FirstOrDefault(s => s.Key == key);
        if (existing is null)
        {
            States.Add(new RuleContextState
            {
                Key = key,
                Label = label,
                Group = group,
                Source = RuleContextStateSource.Derived,
                IsSelectable = false,
                IsActive = isActive,
                ValueText = valueText
            });
        }
        else
        {
            existing.Label = label;
            existing.Group = group;
            existing.Source = RuleContextStateSource.Derived;
            existing.IsSelectable = false;
            existing.IsActive = isActive;
            existing.ValueText = valueText;
        }

        NotifyDerivedCollections();
    }

    private void SeedDefaults()
    {
        States.Add(new RuleContextState
        {
            Key = "context.environment.mountain",
            Label = "Gebirge",
            Group = "Umgebung",
            Source = RuleContextStateSource.Manual,
            IsSelectable = true,
            IsActive = false
        });

        States.Add(new RuleContextState
        {
            Key = "context.status.wet",
            Label = "Nass",
            Group = "Zustand",
            Source = RuleContextStateSource.Manual,
            IsSelectable = true,
            IsActive = false
        });

        States.Add(new RuleContextState
        {
            Key = "derived.encumbrance",
            Label = "Belastet",
            Group = "Berechnet",
            Source = RuleContextStateSource.Derived,
            IsSelectable = false,
            IsActive = true,
            ValueText = "Stufe 2"
        });

        NotifyDerivedCollections();
    }

    private void NotifyDerivedCollections()
    {
        OnPropertyChanged(nameof(ManualStates));
        OnPropertyChanged(nameof(DerivedStates));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
