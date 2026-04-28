using VademecumDigitalis.Models.RuleEngine;

namespace VademecumDigitalis.Controls;

public partial class RuleContextSelectorView : ContentView
{
    public static readonly BindableProperty ManualStatesProperty = BindableProperty.Create(
        nameof(ManualStates),
        typeof(IEnumerable<RuleContextState>),
        typeof(RuleContextSelectorView),
        defaultValue: Array.Empty<RuleContextState>());

    public static readonly BindableProperty DerivedStatesProperty = BindableProperty.Create(
        nameof(DerivedStates),
        typeof(IEnumerable<RuleContextState>),
        typeof(RuleContextSelectorView),
        defaultValue: Array.Empty<RuleContextState>());

    public static readonly BindableProperty ShowManualHeaderProperty = BindableProperty.Create(
        nameof(ShowManualHeader),
        typeof(bool),
        typeof(RuleContextSelectorView),
        true);

    public static readonly BindableProperty ShowDerivedHeaderProperty = BindableProperty.Create(
        nameof(ShowDerivedHeader),
        typeof(bool),
        typeof(RuleContextSelectorView),
        true);

    public IEnumerable<RuleContextState> ManualStates
    {
        get => (IEnumerable<RuleContextState>)GetValue(ManualStatesProperty);
        set => SetValue(ManualStatesProperty, value);
    }

    public IEnumerable<RuleContextState> DerivedStates
    {
        get => (IEnumerable<RuleContextState>)GetValue(DerivedStatesProperty);
        set => SetValue(DerivedStatesProperty, value);
    }

    public bool ShowManualHeader
    {
        get => (bool)GetValue(ShowManualHeaderProperty);
        set => SetValue(ShowManualHeaderProperty, value);
    }

    public bool ShowDerivedHeader
    {
        get => (bool)GetValue(ShowDerivedHeaderProperty);
        set => SetValue(ShowDerivedHeaderProperty, value);
    }

    public RuleContextSelectorView()
    {
        InitializeComponent();
    }
}
