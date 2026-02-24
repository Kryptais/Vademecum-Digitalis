namespace VademecumDigitalis;

using VademecumDigitalis.Services;
using VademecumDigitalis.ViewModels;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>();

    // register services and viewmodels
    builder.Services.AddSingleton<InventoryService>();
    builder.Services.AddSingleton<InventoryViewModel>();
    builder.Services.AddSingleton<InventoryLogService>();

        return builder.Build();
    }
}
