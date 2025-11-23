using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoCompare
{
    /// <summary>
    /// Logger-klassen hanterar loggning av händelser i systemet.
    /// Loggar sparas i JSON-format i en fil, och kan både läggas till och visas.
    /// </summary>
    public class Logger
    {
        private readonly string _filePath;
        private List<Dictionary<string, string>> _logs;

        // Konstruktor: tar emot filväg och laddar befintliga loggar om filen finns
        public Logger(string filePath)
        {
            _filePath = filePath;
            _logs = LoadLogs();
        }

        // Logger.Log() → sparar felet till JSON
        public void Log(string user, string context, string message)
        {
            var entry = new Dictionary<string, string>
            {
                { "Timestamp", DateTime.UtcNow.ToString("o") },
                { "User", user },
                { "Context", context },
                { "Message", message }
            };

            _logs.Add(entry);
            SaveLogs();
        }

        // Hämta alla loggar som lista
        public List<Dictionary<string, string>> GetLogs() => _logs;

        // Spara loggar till fil

        public void DisplayLogs()
        {
            Console.WriteLine("\n--- System Log ---");

            // Loopar igenom varje loggpost och skriver ut
            foreach (var entry in _logs)
            {
                Console.WriteLine($"{entry["Timestamp"]} | {entry["User"]} | {entry["Action"]}");
            }
        }

        private void SaveLogs()
        {
            var json = JsonSerializer.Serialize(_logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        // Ladda loggar från fil
        private List<Dictionary<string, string>> LoadLogs()
        {
            if (!File.Exists(_filePath))
                return new List<Dictionary<string, string>>();

            try
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>();
            }
            catch
            {
                // Om filen är korrupt returnera tom lista
                return new List<Dictionary<string, string>>();
            }
        }
    }
}
