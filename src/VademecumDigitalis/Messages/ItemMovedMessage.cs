using VademecumDigitalis.Models;

namespace VademecumDigitalis.Messages
{
    public class ItemMovedMessage
    {
        public InventoryItem Item { get; }
        public InventoryContainer From { get; }
        public InventoryContainer To { get; }

        public ItemMovedMessage(InventoryItem item, InventoryContainer from, InventoryContainer to)
        {
            Item = item;
            From = from;
            To = to;
        }
    }
}
