using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis;

public partial class CombatTalentsPage : ContentPage
{
    private bool _isInitialized = false;

    public CombatTalentsPage()
    {
        InitializeComponent();
        BindingContext = CharacterSheetSession.Current;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Warte bis die Page vollständig geladen ist
        if (!_isInitialized)
        {
            _isInitialized = true;
            SizeChanged += OnPageSizeChanged;

            // Initiale Größenanpassung mit Delay, damit Width korrekt ist
            Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(100), () =>
            {
                OnPageSizeChanged(this, EventArgs.Empty);
            });
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        SizeChanged -= OnPageSizeChanged;
    }

    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        double width = Width;

        // Ignoriere ungültige Werte
        if (width <= 100) return;

        // Bestimme Breakpoints: Small (0-599), Medium (600-999), Large (1000+)
        ResponsiveSize size = width switch
        {
            < 600 => ResponsiveSize.Small,
            < 1000 => ResponsiveSize.Medium,
            _ => ResponsiveSize.Large
        };

        ApplyResponsiveStyles(size);
    }

    private void ApplyResponsiveStyles(ResponsiveSize size)
    {
        // Haupt-Container
        if (this.Content is ScrollView scrollView && scrollView.Content is VerticalStackLayout mainStack)
        {
            mainStack.Padding = size switch
            {
                ResponsiveSize.Small => new Thickness(8),
                ResponsiveSize.Medium => new Thickness(12),
                _ => new Thickness(16)
            };

            mainStack.Spacing = size switch
            {
                ResponsiveSize.Small => 6,
                ResponsiveSize.Medium => 8,
                _ => 10
            };

            // Durchlaufe alle Child-Elemente
            foreach (var child in mainStack.Children)
            {
                ApplyResponsiveStylesRecursive(child, size);
            }
        }
    }

    private void ApplyResponsiveStylesRecursive(IView element, ResponsiveSize size)
    {
        switch (element)
        {
            case Border border:
                border.Padding = size switch
                {
                    ResponsiveSize.Small => new Thickness(8, 6),
                    ResponsiveSize.Medium => new Thickness(10, 7),
                    _ => new Thickness(12, 8)
                };

                if (border.Content is IView borderContent)
                    ApplyResponsiveStylesRecursive(borderContent, size);
                break;

            case VerticalStackLayout vStack:
                vStack.Spacing = size switch
                {
                    ResponsiveSize.Small => 4,
                    ResponsiveSize.Medium => 5,
                    _ => 6
                };

                foreach (var child in vStack.Children)
                    ApplyResponsiveStylesRecursive(child, size);
                break;

            case Grid grid:
                grid.ColumnSpacing = size switch
                {
                    ResponsiveSize.Small when grid.BackgroundColor == Color.FromArgb("#0E7490") => 2, // Kampftechniken Header
                    ResponsiveSize.Medium when grid.BackgroundColor == Color.FromArgb("#0E7490") => 3,
                    ResponsiveSize.Small when grid.BackgroundColor == Color.FromArgb("#FF6F00") => 1, // Status Header
                    ResponsiveSize.Medium when grid.BackgroundColor == Color.FromArgb("#FF6F00") => 1.5,
                    ResponsiveSize.Small => 4,
                    ResponsiveSize.Medium => 6,
                    _ => 8
                };

                grid.RowSpacing = size switch
                {
                    ResponsiveSize.Small => 2,
                    ResponsiveSize.Medium => 3,
                    _ => 4
                };

                grid.Padding = size switch
                {
                    ResponsiveSize.Small when grid.BackgroundColor == Color.FromArgb("#0E7490") => new Thickness(8, 6),
                    ResponsiveSize.Medium when grid.BackgroundColor == Color.FromArgb("#0E7490") => new Thickness(10, 7),
                    ResponsiveSize.Small when grid.BackgroundColor == Color.FromArgb("#FF6F00") => new Thickness(6, 3),
                    ResponsiveSize.Medium when grid.BackgroundColor == Color.FromArgb("#FF6F00") => new Thickness(7, 3.5),
                    ResponsiveSize.Small when grid.BackgroundColor == Color.FromArgb("#222222") => new Thickness(8, 7),
                    ResponsiveSize.Medium when grid.BackgroundColor == Color.FromArgb("#222222") => new Thickness(10, 8.5),
                    ResponsiveSize.Small when grid.BackgroundColor == Color.FromArgb("#2a2a3e") => new Thickness(6, 4),
                    ResponsiveSize.Medium when grid.BackgroundColor == Color.FromArgb("#2a2a3e") => new Thickness(7, 5),
                    _ => grid.Padding
                };

                foreach (var child in grid.Children)
                    ApplyResponsiveStylesRecursive(child, size);
                break;

            case Label label:
                // Basiere FontSize auf aktueller Größe
                if (label.FontSize >= 18 && label.FontSize <= 20)
                {
                    label.FontSize = size switch
                    {
                        ResponsiveSize.Small => label.FontAttributes.HasFlag(FontAttributes.Bold) ? 14 : 12,
                        ResponsiveSize.Medium => label.FontAttributes.HasFlag(FontAttributes.Bold) ? 17 : 15,
                        _ => 20
                    };
                }
                else if (label.FontSize >= 16 && label.FontSize < 18)
                {
                    label.FontSize = size switch
                    {
                        ResponsiveSize.Small => 12,
                        ResponsiveSize.Medium => 14,
                        _ => 16
                    };
                }
                else if (label.FontSize >= 13 && label.FontSize < 16)
                {
                    label.FontSize = size switch
                    {
                        ResponsiveSize.Small => 11,
                        ResponsiveSize.Medium => 14,
                        _ => 16
                    };
                }

                // Text-Abkürzungen für kleine Bildschirme
                if (size == ResponsiveSize.Small)
                {
                    if (label.Text == "Ausweichen") label.Text = "AW";
                    else if (label.Text == "Initiative") label.Text = "INI";
                    else if (label.Text == "Seelenkraft") label.Text = "SK";
                    else if (label.Text == "Zähigkeit") label.Text = "ZK";
                    else if (label.Text == "Kampftechnik") label.Text = "KT";
                    else if (label.Text == "Leiteigenschaft") label.Text = "LE";
                    else if (label.Text == "Boni") label.Text = "Bo";
                    else if (label.Text == "Gesamt") label.Text = "Ges";
                }
                else if (size == ResponsiveSize.Medium)
                {
                    if (label.Text == "AW") label.Text = "Ausw.";
                    else if (label.Text == "INI") label.Text = "Init.";
                    else if (label.Text == "SK") label.Text = "Seelenk.";
                    else if (label.Text == "ZK") label.Text = "Zäh.";
                    else if (label.Text == "KT") label.Text = "Kampftechnik";
                    else if (label.Text == "LE") label.Text = "Leitei.";
                    else if (label.Text == "Bo") label.Text = "Bon";
                    else if (label.Text == "Ges") label.Text = "Ges.";
                }
                else // Large
                {
                    if (label.Text == "Ausw.") label.Text = "Ausweichen";
                    else if (label.Text == "Init.") label.Text = "Initiative";
                    else if (label.Text == "Seelenk.") label.Text = "Seelenkraft";
                    else if (label.Text == "Zäh.") label.Text = "Zähigkeit";
                    else if (label.Text == "Leitei.") label.Text = "Leiteigenschaft";
                    else if (label.Text == "Bon") label.Text = "Boni";
                    else if (label.Text == "Ges.") label.Text = "Gesamt";
                }
                break;

            case Entry entry:
                if (entry.FontSize >= 18)
                {
                    entry.FontSize = size switch
                    {
                        ResponsiveSize.Small => 12,
                        ResponsiveSize.Medium => 15,
                        _ => 18
                    };
                }

                if (entry.WidthRequest == 80 || entry.WidthRequest == 70 || entry.WidthRequest == 60)
                {
                    entry.WidthRequest = size switch
                    {
                        ResponsiveSize.Small => 60,
                        ResponsiveSize.Medium => 70,
                        _ => 80
                    };
                }
                else if (entry.WidthRequest == 50 || entry.WidthRequest == 45 || entry.WidthRequest == 40)
                {
                    entry.WidthRequest = size switch
                    {
                        ResponsiveSize.Small => 40,
                        ResponsiveSize.Medium => 45,
                        _ => 50
                    };
                }
                break;

            case CheckBox checkBox:
                // Dynamische Größenanpassung für CheckBoxen
                var checkBoxSize = size switch
                {
                    ResponsiveSize.Small => 28,
                    ResponsiveSize.Medium => 32,
                    _ => 36
                };
                checkBox.WidthRequest = checkBoxSize;
                checkBox.HeightRequest = checkBoxSize;
                break;
        }
    }

    private void OnStatusButton0Clicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is StatusRow row)
        {
            row.Stufe = 0;
        }
    }

    private void OnStatusButton1Clicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is StatusRow row)
        {
            row.Stufe = 1;
        }
    }

    private void OnStatusButton2Clicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is StatusRow row)
        {
            row.Stufe = 2;
        }
    }

    private void OnStatusButton3Clicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is StatusRow row)
        {
            row.Stufe = 3;
        }
    }

    private void OnStatusButton4Clicked(object? sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is StatusRow row)
        {
            row.Stufe = 4;
        }
    }

    private enum ResponsiveSize
    {
        Small,   // < 600px (Phone-like)
        Medium,  // 600-999px (Tablet-like)
        Large    // >= 1000px (Desktop)
    }
}
