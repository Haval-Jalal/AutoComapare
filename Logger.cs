using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoCompare
{
    /// <summary>
    /// Logger-klassen hanterar loggning av händelser i systemet.
    /// Loggar sparas i JSON-format i en fil, och kan både läggas till och visas.
    /// </summary>
    public class Logger
    {
        // Filvägen där loggar sparas
        private readonly string _filePath;

        // Intern lista som håller alla loggar i minnet
        // Varje logg är en Dictionary med fält: Timestamp, User och Action
        private List<Dictionary<string, string>> _logs;

        /// <summary>
        /// Konstruktor, tar emot filväg och laddar befintliga loggar om filen finns
        /// </summary>
        /// <param name="filePath">Sökväg till JSON-fil där loggar sparas</param>
        public Logger(string filePath)
        {
            _filePath = filePath;
            _logs = LoadLogs(); // Laddar loggar från filen vid start
        }

        /// <summary>
        /// Lägger till en ny loggpost
        /// </summary>
        /// <param name="username">Användarnamn som utförde handlingen</param>
        /// <param name="action">Beskrivning av handlingen</param>
        public void Log(string username, string action)
        {
            // Skapa ett nytt loggobjekt som Dictionary
            var entry = new Dictionary<string, string>
            {
                { "Timestamp", DateTime.UtcNow.ToString("o") }, // UTC-tid i ISO-format
                { "User", username },                             // Vem gjorde handlingen
                { "Action", action }                              // Vad som hände
            };

            // Lägg till loggen i listan
            _logs.Add(entry);

            // Spara loggen direkt till fil
            SaveLogs();
        }

        /// <summary>
        /// Hämtar alla loggar som lista
        /// </summary>
        /// <returns>Lista av Dictionary-objekt med loggar</returns>
        public List<Dictionary<string, string>> GetLogs()
        {
            return _logs;
        }

        /// <summary>
        /// Skriver ut alla loggar till konsolen
        /// </summary>
        public void DisplayLogs()
        {
            Console.WriteLine("\n--- System Log ---");

            // Loopar igenom varje loggpost och skriver ut
            foreach (var entry in _logs)
            {
                Console.WriteLine($"{entry["Timestamp"]} | {entry["User"]} | {entry["Action"]}");
            }
        }

        /// <summary>
        /// Sparar alla loggar i JSON-format till fil
        /// </summary>
        private void SaveLogs()
        {
            // Serialiserar listan till JSON med indrag för läsbarhet
            var json = JsonSerializer.Serialize(_logs, new JsonSerializerOptions { WriteIndented = true });

            // Skriver JSON-strängen till filen
            File.WriteAllText(_filePath, json);
        }

        /// <summary>
        /// Laddar loggar från JSON-fil, eller returnerar en tom lista om filen inte finns
        /// </summary>
        /// <returns>Lista av loggar</returns>
        private List<Dictionary<string, string>> LoadLogs()
        {
            // Om filen inte finns, returnera en tom lista
            if (!File.Exists(_filePath))
                return new List<Dictionary<string, string>>();

            // Läs hela JSON-filen som text
            var json = File.ReadAllText(_filePath);

            // Deserialisera JSON till lista av Dictionary
            // Om filen är tom eller korrupt, returnera tom lista
            return JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json) ?? new List<Dictionary<string, string>>();
        }
    }

}
