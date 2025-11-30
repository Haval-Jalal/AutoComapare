using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoCompare
{
    public class DataStore<T>
    {
        // List that holds objects of type T
        public List<T> List { get; set; } = new List<T>();

        public string Filename { get; private set; }

        public DataStore(string filename = null)
        {
            Filename = filename ?? GetDefaultFilename();
        }

        private string GetDefaultFilename()
        {
            return $"{typeof(T).Name.ToLower()}s.json"; 
        }

        //Adds object of the Type T
        public void AddItem(T item)
        {
            List.Add(item);
            SaveToJson(); 
        }

        //Remove object of the Type T
        public bool RemoveItem(T item)
        {
            bool removed = List.Remove(item);
            if (removed)
                SaveToJson();
            return removed;
        }

      
        // Saving to Json 
        public void SaveToJson()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNameCaseInsensitive = true,
                };
                
                var json = JsonSerializer.Serialize(List, options);
                File.WriteAllText(Filename, json);
            }
            catch (Exception ex)
            {
                Logger.Log($"SaveToJson failed for file '{Filename}", ex);
            }
        }

        // Loading from Json file 
        public void LoadFromJson()
        {
            try
            {
                if (!File.Exists(Filename))
                {
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