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
    builder.Services.AddSingleton<PersistenceService>();
    builder.Services.AddSingleton<InventoryService>();
    builder.Services.AddSingleton<InventoryViewModel>();
    builder.Services.AddSingleton<InventoryLogService>();
    builder.Services.AddSingleton<IDialogService, DialogService>();
    builder.Services.AddSingleton<MainPageViewModel>();
    builder.Services.AddSingleton<BoronKalenderViewModel>();
    
    // Pages + VMs
    builder.Services.AddTransient<InventoryContainerPage>();
    builder.Services.AddTransient<InventoryContainerViewModel>();
    
    builder.Services.AddTransient<GlobalItemSearchPage>();
    builder.Services.AddTransient<GlobalItemSearchViewModel>();

    builder.Services.AddTransient<BoronKalenderPage>();

        return builder.Build();
    }
}
