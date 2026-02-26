using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using VademecumDigitalis.Models;
using Microsoft.Maui.Storage;

namespace VademecumDigitalis.Services
{
    public class PersistenceService
    {
        private const string InventoryFileName = "inventory_data.json";

        private string GetInventoryFilePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, InventoryFileName);
        }

        public async Task SaveInventoryAsync(IEnumerable<InventoryContainer> containers)
        {
            try
            {
                var filePath = GetInventoryFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };
                
                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, containers, options);
            }
            catch (Exception ex)
            {
                // In a real app, you might want to log this properly
                System.Diagnostics.Debug.WriteLine($"Error saving inventory: {ex.Message}");
            }
        }

        public async Task<List<InventoryContainer>> LoadInventoryAsync()
        {
            try
            {
                var filePath = GetInventoryFilePath();
                if (!File.Exists(filePath))
                {
                    return new List<InventoryContainer>();
                }

                using var stream = File.OpenRead(filePath);
                var result = await JsonSerializer.DeserializeAsync<List<InventoryContainer>>(stream);
                return result ?? new List<InventoryContainer>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading inventory: {ex.Message}");
                return new List<InventoryContainer>();
            }
        }
    }
}
