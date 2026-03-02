using VademecumDigitalis.Models;

namespace VademecumDigitalis.Messages
{
    public class MoneyTransferredMessage
    {
        public InventoryContainer From { get; }
        public InventoryContainer To { get; }

        public MoneyTransferredMessage(InventoryContainer from, InventoryContainer to)
        {
            From = from;
            To = to;
        }
    }
}
