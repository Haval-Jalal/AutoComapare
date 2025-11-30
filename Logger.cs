using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace AutoCompare
{
    //static
    public static class Logger
    {
        //a file path to store logs in logs.json
        private static readonly string _filePath = "logs.json";
        private static List<Dictionary<string, string>> _logs = new List<Dictionary<string, string>>();

        //constructor, so that _logs list is loaded when Logger is used
        //don’t need to create an instance of Logger
        static Logger()
        {
            _logs = LoadLogs();
        }

        //Connected to _logs list with all the logs
        // A log is a logpost with errorScope, timestamp and error message
        public static void Log(string errorScope, Exception ex)
        {
            var entry = new Dictionary<string, string>
            {
                { "Scope", errorScope},
                { "TimeStamp", DateTime.Now.ToString ("o") },
                { "ErrorMessage", ex.ToString() }
            };
            _logs.Add(entry);
            SaveLogs();
        }

        //Gives access to the logs thats already in the _logs list
        public static List<Dictionary<string, string>> GetLogs() => _logs;

        //writes up all the logs in the console
        public static void DisplayLogs()
        {
            Console.WriteLine("\n--- System Log ---");
            foreach (var entry in _logs)
            {
                Console.WriteLine($"{entry["Scope"]} | {entry["TimeStamp"]} | {entry["ErrorMessage"]}");
            }
        }

        //_logs saves to logs.json file
        private static void SaveLogs()
        {
            var json = JsonSerializer.Serialize(_logs, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        //loads logs from logs.json file to _logs list
        private static List<Dictionary<string, string>> LoadLogs()
        {
            if (!File.Exists(_filePath))
                return new List<Dictionary<string, string>>();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>();
        }
    }
}