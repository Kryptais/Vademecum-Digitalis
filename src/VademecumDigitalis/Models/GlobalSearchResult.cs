namespace VademecumDigitalis.Models
{
    public class GlobalSearchResult
    {
        public InventoryItem Item { get; }
        public InventoryContainer Container { get; }
        
        public string ItemName => Item.Name;
        public string ContainerName => Container.Name;
        public string QuantityText => $"{Item.Quantity}x";

        public GlobalSearchResult(InventoryItem item, InventoryContainer container)
        {
            Item = item;
            Container = container;
        }
    }
}
