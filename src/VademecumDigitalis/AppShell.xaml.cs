namespace VademecumDigitalis;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
    }

    private void OnDashboardClicked(object? sender, EventArgs e)
    {
        if (Application.Current is App app && Handler?.MauiContext?.Services is { } services)
            app.SwitchToDashboard(services);
    }
}
