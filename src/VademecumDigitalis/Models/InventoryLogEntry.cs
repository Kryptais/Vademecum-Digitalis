using System;

namespace VademecumDigitalis.Models
{
    public class InventoryLogEntry
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Message { get; set; } = string.Empty;
        public override string ToString() => $"[{Timestamp:u}] {Message}";
    }
}
