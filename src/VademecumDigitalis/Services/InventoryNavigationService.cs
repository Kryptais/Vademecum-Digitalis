using Microsoft.Extensions.DependencyInjection;
using VademecumDigitalis.Models;
using VademecumDigitalis.ViewModels;

namespace VademecumDigitalis.Services;

public sealed class InventoryNavigationService : IInventoryNavigationService
{
    private readonly IServiceProvider _services;

    public InventoryNavigationService(IServiceProvider services)
    {
        _services = services;
    }

    public async Task NavigateToContainerAsync(InventoryContainer container)
    {
        ArgumentNullException.ThrowIfNull(container);

        var page = _services.GetRequiredService<InventoryContainerPage>();
        if (page.BindingContext is InventoryContainerViewModel vm)
        {
            vm.Container = container;
        }

        await MainPage.Navigation.PushAsync(page);
    }

    public async Task NavigateToGlobalSearchAsync()
    {
        var page = _services.GetRequiredService<GlobalItemSearchPage>();
        await MainPage.Navigation.PushAsync(page);
    }

    public Task NavigateBackAsync()
    {
        return MainPage.Navigation.PopAsync();
    }

    public async Task<InventoryItem?> AddItemAsync()
    {
        var page = CreateAddItemPage();
        await MainPage.Navigation.PushModalAsync(new NavigationPage(page));
        await WaitForCloseAsync(page);
        return page.ResultItem;
    }

    public async Task EditItemAsync(InventoryItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var page = CreateAddItemPage();
        page.SetEditingItem(item);
        await MainPage.Navigation.PushModalAsync(new NavigationPage(page));
        await WaitForCloseAsync(page);
    }

    public async Task<MoneyTransferResult?> RequestMoneyTransferAsync(InventoryContainer source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var vm = _services.GetRequiredService<MoneyTransferViewModel>();
        vm.SetSource(source);
        var page = new MoneyTransferPage(vm);

        await MainPage.Navigation.PushModalAsync(page);
        await WaitForCloseAsync(page);

        return page.Confirmed
            ? new MoneyTransferResult(page.Dukaten, page.Silbertaler, page.Heller, page.Kreuzer)
            : null;
    }

    private InventoryAddItemPage CreateAddItemPage()
    {
        return new InventoryAddItemPage(_services.GetRequiredService<InventoryAddItemViewModel>());
    }

    private static Task WaitForCloseAsync(Page page)
    {
        var completion = new TaskCompletionSource<object?>();
        page.Disappearing += OnDisappearing;
        return completion.Task;

        void OnDisappearing(object? sender, EventArgs args)
        {
            page.Disappearing -= OnDisappearing;
            completion.TrySetResult(null);
        }
    }

    private static Page MainPage => Application.Current?.MainPage
        ?? throw new InvalidOperationException("No main page available.");
}
