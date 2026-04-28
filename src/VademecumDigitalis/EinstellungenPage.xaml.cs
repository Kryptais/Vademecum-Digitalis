namespace VademecumDigitalis;

public partial class EinstellungenPage : ContentPage
{
    private readonly IServiceProvider _services;

    public EinstellungenPage(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    private void OnDashboardClicked(object sender, EventArgs e)
    {
        if (Application.Current is App app)
            app.SwitchToDashboard(_services);
    }
}
