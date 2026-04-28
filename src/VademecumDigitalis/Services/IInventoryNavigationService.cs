using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

public interface IInventoryNavigationService
{
    Task NavigateToContainerAsync(InventoryContainer container);

    Task NavigateToGlobalSearchAsync();

    Task NavigateBackAsync();

    Task<InventoryItem?> AddItemAsync();

    Task EditItemAsync(InventoryItem item);

    Task<MoneyTransferResult?> RequestMoneyTransferAsync(InventoryContainer source);
}
