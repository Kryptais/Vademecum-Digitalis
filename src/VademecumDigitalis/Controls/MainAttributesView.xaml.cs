using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis.Controls;

public partial class MainAttributesView : ContentView
{
    public static readonly BindableProperty EditableProperty =
        BindableProperty.Create(
            nameof(Editable),
            typeof(bool),
            typeof(MainAttributesView),
            defaultValue: false);

    /// <summary>If true, attributes are editable via Entry fields. Otherwise read-only labels.</summary>
    public bool Editable
    {
        get => (bool)GetValue(EditableProperty);
        set => SetValue(EditableProperty, value);
    }

    public MainAttributesView()
    {
        InitializeComponent();

        // Default-BindingContext = aktuelle Charakter-Session, falls nicht überschrieben.
        if (BindingContext is null)
        {
            BindingContext = CharacterSheetSession.Current;
        }
    }
}
