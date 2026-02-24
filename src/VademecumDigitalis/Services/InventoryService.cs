using System;
using System.Linq;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services
{
    public class InventoryService
    {
        public void MoveItem(InventoryContainer from, InventoryContainer to, InventoryItem item, int quantity)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (quantity <= 0) throw new ArgumentException("Quantity must be positive.", nameof(quantity));

            var sourceItem = from.Items.FirstOrDefault(i => i.Id == item.Id);
            if (sourceItem == null) throw new InvalidOperationException("Item not found in source container.");
            if (sourceItem.Quantity < quantity) throw new InvalidOperationException("Not enough quantity in source container.");

            // Decrease or remove from source
            sourceItem.Quantity -= quantity;
            if (sourceItem.Quantity == 0)
            {
                from.Items.Remove(sourceItem);
            }

            // Add to destination (try to merge by name+weight)
            var destItem = to.Items.FirstOrDefault(i => i.Name == item.Name && Math.Abs(i.WeightPerUnit - item.WeightPerUnit) < 0.0001);
            if (destItem != null)
            {
                destItem.Quantity += quantity;
            }
            else
            {
                var newItem = new InventoryItem
                {
                    Name = item.Name,
                    WeightPerUnit = item.WeightPerUnit,
                    Quantity = quantity,
                    Details = item.Details,
                    AcquiredDate = item.AcquiredDate,
                    Tags = new System.Collections.Generic.List<string>(item.Tags)
                };
                to.Items.Add(newItem);
            }
        }

        public void TransferMoney(InventoryContainer from, InventoryContainer to, int dukaten = 0, int silbertaler = 0, int heller = 0, int kreuzer = 0)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            from.Money.TransferTo(to.Money, dukaten, silbertaler, heller, kreuzer);
        }
    }
}
