using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VademecumDigitalis.Models.RuleEngine;

public enum RuleContextStateSource
{
    Manual,
    Derived
}

/// <summary>
/// Laufzeit-Zustand einer Condition für die Charakterberechnung.
/// </summary>
public class RuleContextState : INotifyPropertyChanged
{
    private bool _isActive;
    private bool _isSelectable = true;
    private string _valueText = string.Empty;

    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;

    /// <summary>Kurzer Detailtext, z. B. "Belastung 2".</summary>
    public string ValueText
    {
        get => _valueText;
        set
        {
            if (_valueText != value)
            {
                _valueText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Anzeige));
            }
        }
    }

    public RuleContextStateSource Source { get; set; }

    /// <summary>True wenn der Zustand aktiv in die Regelberechnung einfließt.</summary>
    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive != value)
            {
                _isActive = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Anzeige));
            }
        }
    }

    /// <summary>False wenn Zustand berechnet/gesperrt ist und nicht manuell geschaltet werden darf.</summary>
    public bool IsSelectable
    {
        get => _isSelectable;
        set
        {
            if (_isSelectable != value)
            {
                _isSelectable = value;
                OnPropertyChanged();
            }
        }
    }

    public string Anzeige => string.IsNullOrWhiteSpace(ValueText) ? Label : $"{Label} ({ValueText})";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
