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
    
    // Pages + VMs
    builder.Services.AddTransient<InventoryContainerPage>();
    builder.Services.AddTransient<InventoryContainerViewModel>();
    
    builder.Services.AddTransient<GlobalItemSearchPage>();
    builder.Services.AddTransient<GlobalItemSearchViewModel>();

        return builder.Build();
    }
}
