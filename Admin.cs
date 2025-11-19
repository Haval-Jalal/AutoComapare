using Spectre.Console;
using System;
using System.IO;
using System.Linq;

namespace AutoCompare
{
    public class Admin
    {
        private readonly DataStore<User> _userStore;
        private readonly Logger _logger;

        private const string AdminUsername = "admin.autocompare@gmail.com";
        private const string AdminPassword = "Admin123!";

        public bool IsLoggedIn { get; private set; }

        public Admin(DataStore<User> userStore, Logger logger)
        {
            _userStore = userStore;
            _logger = logger;
        }

        public bool TryLogin(string username, string password)
        {

            if (username == AdminUsername && password == AdminPassword)
            {
                IsLoggedIn = true;
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Admin access granted.");
                Console.ResetColor();
                return true;
            }
            return false;
        }

        // NEW: Spectre.Console login prompt for the admin panel
        public bool TryLoginPrompt()
        {
            AnsiConsole.Clear();

            var panel = new Panel("[yellow]Admin Login Required[/]")
                .Border(BoxBorder.Rounded)
                .Header("[red]ADMIN ACCESS[/]", Justify.Center)
                .Padding(1, 1, 1, 1);

            AnsiConsole.Write(panel);

            // Ask for admin username
            string username = AnsiConsole.Ask<string>("[green]Enter admin username:[/]").Trim();

            // Ask for admin password securely
            string password = AnsiConsole.Prompt(
                new TextPrompt<string>("[green]Enter admin password:[/]")
                    .Secret()
                    .PromptStyle("red"));

            // Validate using existing TryLogin method
            if (TryLogin(username, password))
            {
                AnsiConsole.MarkupLine("[green]Admin access granted![/]");
                Thread.Sleep(700);
                return true;
            }
            else
            {
                AnsiConsole.MarkupLine("[red]Invalid admin credentials.[/]");
                Thread.Sleep(1000);
                return false;
            }
        }

        public void ShowAllUsers()
        {
            Console.WriteLine("\n=== ALL USERS ===");
            foreach (var user in _userStore.List)
                Console.WriteLine($"• {user.Username} (Registered: {user.RegisteredAt})");
            Console.WriteLine("=================\n");
        }

        public void DeleteUser(string username)
        {
            var user = _userStore.List.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null)
            {
                Console.WriteLine("User not found.");
                return;
            }
            _userStore.RemoveItem(user); // Save is handled by DataStore
            Console.WriteLine($"User '{username}' has been deleted.");
            _logger.Log("admin", $"Deleted user: {username}");
        }

        public void ShowLogFiles()
        {
            Console.WriteLine("\n=== LOG FILES ===");
            if (!Directory.Exists("logs"))
            {
                Console.WriteLine("No log directory found.");
                return;
            }
            var files = Directory.GetFiles("logs", "*.json");
            if (!files.Any())
            {
                Console.WriteLine("No log files found.");
                return;
            }
            foreach (var file in files)
                Console.WriteLine("• " + Path.GetFileName(file));
            Console.WriteLine("==================\n");
        }

        public void ShowLogEntries(string filename)
        {
            string path = Path.Combine("logs", filename);
            if (!File.Exists(path))
            {
                Console.WriteLine("Log file does not exist.");
                return;
            }

            Console.WriteLine($"\n=== CONTENT OF {filename} ===");
            foreach (var line in File.ReadAllLines(path))
                Console.WriteLine(line);
            Console.WriteLine("==============================\n");
        }
    }
}