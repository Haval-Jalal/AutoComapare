using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoCompare
{
    public class Logger
    {
        private static readonly string _filePath = "logs.json";
        private static List<Dictionary<string, string>> _logs = new List<Dictionary<string, string>>();

        
        static Logger()
        {
            _logs = LoadLogs();
        }

        //public static void Log(string username, string action)
        public static void Log(string errorScope, Exception ex)
        {
            var entry = new Dictionary<string, string>
            {
                {"Scope", errorScope },
                { "Timestamp", DateTime.UtcNow.ToString("o") },
                { "Error Message", ex.ToString() }
            };
            _logs.Add(entry);
            SaveLogs();
        }

        public static List<Dictionary<string, string>> GetLogs() => _logs;

        public static void DisplayLogs()
        {
            Console.WriteLine("\n--- System Log ---");
            foreach (var entry in _logs)
            {
                Console.WriteLine($"{entry["Timestamp"]} | {entry["User"]} | {entry["Action"]}");
            }
        }

        private static void SaveLogs()
        {
            var json = JsonSerializer.Serialize(_logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        private static List<Dictionary<string, string>> LoadLogs()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException();
                return new List<Dictionary<string, string>>();
            

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>();
        }
    }
}