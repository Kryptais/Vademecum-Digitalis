using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services;

public interface IInventoryService
{
    void MoveItem(InventoryContainer from, InventoryContainer to, InventoryItem item, int quantity);

    void TransferMoney(
        InventoryContainer from,
        InventoryContainer to,
        int dukaten = 0,
        int silbertaler = 0,
        int heller = 0,
        int kreuzer = 0);
}
