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
        private const string CharacterSheetFileName = "charactersheet_data.json";
        private const string KalenderFileName = "kalender_data.json";

        private string GetInventoryFilePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, InventoryFileName);
        }

        private string GetCharacterSheetFilePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, CharacterSheetFileName);
        }

        private string GetKalenderFilePath()
        {
            return Path.Combine(FileSystem.AppDataDirectory, KalenderFileName);
        }

        // --- Inventory ---

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

        // --- Character Sheet ---

        public async Task SaveCharacterSheetAsync(CharacterSheetData data)
        {
            try
            {
                var filePath = GetCharacterSheetFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };

                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, data, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving character sheet: {ex.Message}");
            }
        }

        public async Task<CharacterSheetData?> LoadCharacterSheetAsync()
        {
            try
            {
                var filePath = GetCharacterSheetFilePath();
                if (!File.Exists(filePath))
                {
                    return null;
                }

                using var stream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<CharacterSheetData>(stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading character sheet: {ex.Message}");
                return null;
            }
        }

        // --- Kalender ---

        public async Task SaveKalenderAsync(KalenderData data)
        {
            try
            {
                var filePath = GetKalenderFilePath();
                var options = new JsonSerializerOptions { WriteIndented = true };

                using var stream = File.Create(filePath);
                await JsonSerializer.SerializeAsync(stream, data, options);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving calendar: {ex.Message}");
            }
        }

        public async Task<KalenderData?> LoadKalenderAsync()
        {
            try
            {
                var filePath = GetKalenderFilePath();
                if (!File.Exists(filePath))
                    return null;

                using var stream = File.OpenRead(filePath);
                return await JsonSerializer.DeserializeAsync<KalenderData>(stream);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading calendar: {ex.Message}");
                return null;
            }
        }
    }
}
