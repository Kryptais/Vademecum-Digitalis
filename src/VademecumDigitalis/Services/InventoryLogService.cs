using System;
using System.Collections.ObjectModel;
using System.IO;
using VademecumDigitalis.Models;

namespace VademecumDigitalis.Services
{
    public class InventoryLogService
    {
        private readonly string _logFile;
        public ObservableCollection<InventoryLogEntry> Entries { get; } = new ObservableCollection<InventoryLogEntry>();

        public InventoryLogService()
        {
            var baseDir = AppContext.BaseDirectory ?? Environment.CurrentDirectory;
            _logFile = Path.Combine(baseDir, "inventory.log");
            // load existing
            if (File.Exists(_logFile))
            {
                foreach (var line in File.ReadAllLines(_logFile))
                {
                    Entries.Add(new InventoryLogEntry { Message = line, Timestamp = DateTime.UtcNow });
                }
            }
        }

        public void Append(string message)
        {
            var entry = new InventoryLogEntry { Message = message, Timestamp = DateTime.UtcNow };
            Entries.Add(entry);
            try
            {
                File.AppendAllText(_logFile, entry.ToString() + Environment.NewLine);
            }
            catch
            {
                // ignore IO issues for now
            }
        }
    }
}
