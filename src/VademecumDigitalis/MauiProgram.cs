namespace VademecumDigitalis;

using VademecumDigitalis.Services;
using VademecumDigitalis.ViewModels;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        // --- Services ---
        builder.Services.AddSingleton<PersistenceService>();
        builder.Services.AddSingleton<InventoryService>();
        builder.Services.AddSingleton<InventoryLogService>();
        builder.Services.AddSingleton<IDialogService, DialogService>();
        builder.Services.AddSingleton<AdvantagesService>();
        builder.Services.AddSingleton<SpecialAbilityService>();
        builder.Services.AddSingleton<VorteilNachteilService>();
        builder.Services.AddSingleton<TalentModifierService>();
        builder.Services.AddSingleton<TalentCatalogService>();
        builder.Services.AddSingleton<CharacterSaveService>();

        // --- ViewModels (Singleton = geteilt pro Session) ---
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<BoronKalenderViewModel>();
        builder.Services.AddSingleton<AdvantagesViewModel>();
        builder.Services.AddSingleton<InventoryViewModel>();
        builder.Services.AddSingleton<DashboardViewModel>();

        // --- ViewModels (Transient = frische Instanz pro Page) ---
        builder.Services.AddTransient<InventoryContainerViewModel>();
        builder.Services.AddTransient<GlobalItemSearchViewModel>();
        builder.Services.AddTransient<SonderfertigkeitenViewModel>();
        builder.Services.AddTransient<VorteilNachteilViewModel>();
        builder.Services.AddTransient<RegelnViewModel>();
        builder.Services.AddTransient<MoneyTransferViewModel>();

        // --- Pages ---
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<EinstellungenPage>();
        builder.Services.AddTransient<RegelnPage>();
        builder.Services.AddSingleton<AdvantagesPage>();
        builder.Services.AddTransient<SonderfertigkeitenPage>();
        builder.Services.AddTransient<BoronKalenderPage>();
        builder.Services.AddTransient<EreignissePage>();
        builder.Services.AddTransient<InventoryContainerPage>();
        builder.Services.AddTransient<GlobalItemSearchPage>();

        return builder.Build();
    }
}
