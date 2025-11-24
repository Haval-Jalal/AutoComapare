using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoCompare
{
    public static class Logger
    {
        private static readonly string _filePath;
        private static List<Dictionary<string, string>> _logs = new List<Dictionary<string, string>>();

        // CHANGED: default path is "logs/logs.json" (folder + file), ensures directory exists
        public static Logger(string filePath = "logs.json")
        {
            _filePath = filePath;
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _logs = LoadLogs();
        }

        public static void Log(string username, string action)
        {
            var entry = new Dictionary<string, string>
            {
                { "Timestamp", DateTime.UtcNow.ToString("o") },
                { "User", username },
                { "Action", action }
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
                return new List<Dictionary<string, string>>();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>();
        }
    }
}