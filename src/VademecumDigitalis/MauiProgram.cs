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
    builder.Services.AddSingleton<AdvantagesService>();
    builder.Services.AddSingleton<AdvantagesViewModel>();
    builder.Services.AddSingleton<AdvantagesPage>();
    
    // Pages + VMs
    builder.Services.AddTransient<InventoryContainerPage>();
    builder.Services.AddTransient<InventoryContainerViewModel>();
    
    builder.Services.AddTransient<GlobalItemSearchPage>();
    builder.Services.AddTransient<GlobalItemSearchViewModel>();

    builder.Services.AddTransient<BoronKalenderPage>();
    builder.Services.AddTransient<EreignissePage>();

    builder.Services.AddSingleton<SpecialAbilityService>();
    builder.Services.AddTransient<SonderfertigkeitenViewModel>();
    builder.Services.AddTransient<SonderfertigkeitenPage>();

    builder.Services.AddSingleton<VorteilNachteilService>();
    builder.Services.AddTransient<VorteilNachteilViewModel>();
    builder.Services.AddTransient<AdvantagesPage>();

    builder.Services.AddSingleton<TalentModifierService>();

        return builder.Build();
    }
}
