using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoCompare
{
    public class DataStore<T>
    {
        // The in-memory list that holds objects of type T
        public List<T> List { get; set; } = new List<T>();

        // Optional explicit filename (can be set by caller). If null, default is used.
        public string Filename { get; private set; }

        // NEW: Constructor accepts optional filename override
        public DataStore(string filename = null)
        {
            Filename = filename ?? GetDefaultFilename();
        }

        // CHANGED: centralized default filename generation so it is consistent for all types
        private string GetDefaultFilename()
        {
            return $"{typeof(T).Name.ToLower()}s.json"; // e.g. users.json, cars.json, carsearchs.json
        }

        // Add item and persist immediately
        public void AddItem(T item)
        {
            List.Add(item);
            SaveToJson(); // CHANGED: always save on mutation
        }

        // Remove item and persist immediately
        public bool RemoveItem(T item)
        {
            bool removed = List.Remove(item);
            if (removed)
                SaveToJson();
            return removed;
        }

        public T? FindItem(Predicate<T> predicate)
        {
            return List.Find(predicate);
        }

        // Save using the configured filename
        public void SaveToJson()
        {
            try
            {


                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                    // Preserve enum names as text by default
                };
                var json = JsonSerializer.Serialize(List, options);
                File.WriteAllText(Filename, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"SaveToJson failed for file '{Filename}", ex);
            }
        }



        // Load using the configured filename
        public void LoadFromJson()
        {
            try
            {


                if (!File.Exists(Filename))
                {
                    // NOTE: file missing => keep List as empty (default)
                    List = new List<T>();
                    return;
                }

                var json = File.ReadAllText(Filename);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                List = JsonSerializer.Deserialize<List<T>>(json, options) ?? new List<T>();
            }
            catch (Exception ex)
            {
                Logger.Log($"LoadFromJson failed for file '{Filename}", ex);
                List = new List<T>();
            }
        }
    }
}