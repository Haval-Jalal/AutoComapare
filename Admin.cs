using Spectre.Console;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace AutoCompare
{
    public class Admin
    {
        private readonly DataStore<User> _userStore;

        private const string AdminUsername = "admin.autocompare@gmail.com";
        private const string AdminPassword = "Admin123!";
        public bool IsLoggedIn { get; private set; }

        public Admin(DataStore<User> userStore)
        {
            _userStore = userStore;
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
            ShowHeaderSectionAdmin(
                "User List",
                "Registered Users",
                "Below is the list of all registered users in the system.",
                Color.Blue,
                Color.Yellow,
                Color.White);

            Console.WriteLine("\n=== ALL USERS ===");
            foreach (var user in _userStore.List)
                Console.WriteLine($"• {user.Username} (Registered: {user.RegisteredAt})");
            Console.WriteLine("=================\n");
        }

        public void DeleteUser()
        {
            ShowHeaderSectionAdmin(
                "Delete User",
                "User Deletion",
                "Select a user to delete from the system.",
                Color.Red,
                Color.Yellow,
                Color.White);

            // Get all users
            var users = _userStore.List.ToList();

            if (users.Count == 0)
            {
                Console.WriteLine("No users found.");
                return;
            }
            // Prompt admin to select a user
            foreach (var user in users)
            {
                if (user.Username == AdminUsername)
                {
                    Console.WriteLine("Cannot delete the admin user.");
                    return;
                }
            }

            var userChoices = users.Select((u, index) => $"{index + 1}. {u.Username}").ToList();
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Choose a user to delete:")
                    .PageSize(10)
                    .AddChoices(userChoices)
            );
            int selectedIndex = userChoices.IndexOf(selection);
            var userToDelete = users[selectedIndex];
            // Confirm deletion
            var confirm = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Are you sure you want to delete user [red]{userToDelete.Username}[/]?")
                    .AddChoices("Yes", "No")
            );
            if (confirm == "Yes")
            {
                _userStore.RemoveItem(userToDelete);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"User {userToDelete.Username} has been deleted.");
                Console.ResetColor();
            }
            else
            {
                Console.WriteLine("User deletion cancelled.");
            }
        }

        public void ShowLogFiles()
        {
            ShowHeaderSectionAdmin(
                "Log Files",
                "Available Log Files",
                "Below is the list of available log files in the system.",
                Color.Purple,
                Color.Yellow,
                Color.White);

            Console.WriteLine("\n=== LOG FILES ===");

            bool foundAny = false;

            if (File.Exists("logs.json"))
            {
                Console.WriteLine("• logs.json");
                foundAny = true;
            }

            if (File.Exists("users.json"))
            {
                Console.WriteLine("• users.json");
                foundAny = true;
            }

            if (File.Exists("cars.json"))
            {
                Console.WriteLine("• cars.json");
                foundAny = true;
            }

            if (File.Exists("carsearchs.json"))
            {
                Console.WriteLine("• carsearchs.json");
                foundAny = true;
            }

            if (!foundAny)
            {
                Console.WriteLine("No log files found.");
            }

            Console.WriteLine("==================\n");
        }



        public void ShowLogEntries(string filename)
        {
            ShowHeaderSectionAdmin(
                "Log File Entries",
                $"Contents of {filename}",
                $"Displaying entries from the log file: {filename}",
                Color.Green,
                Color.Yellow,
                Color.White);

            string path = filename;
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
       
        // Display AI chat/search history interactively for all users or a specific user
        public void ShowAiSearchHistoryInteractive()
        {
            ShowHeaderSectionAdmin(
                "AI Search History",
                "View AI Search Logs",
                "Select a user to view their AI search history or view all users' history.",
                Color.Cyan,
                Color.Yellow,
                Color.White);


            // Build user choices
            var userChoices = new List<string> { "All Users" };
            userChoices.AddRange(_userStore.List.Select(u => u.Username));

            // Prompt admin or user to select
            var selectedUser = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Select a user to view AI history (or All Users):[/]")
                    .PageSize(10)
                    .AddChoices(userChoices)
            );

            // Collect search history
            IEnumerable<string> combinedHistory = selectedUser == "All Users"
                ? _userStore.List.SelectMany(u => u.SearchHistory ?? new List<string>())
                : _userStore.List.First(u => u.Username == selectedUser).SearchHistory ?? new List<string>();

            // Order newest first
            combinedHistory = combinedHistory.Reverse();

            if (!combinedHistory.Any())
            {
                AnsiConsole.MarkupLine("[grey]No AI search entries available.[/]");
                AnsiConsole.WriteLine("\nPress any key to continue...");
                Console.ReadKey(true);
                return;
            }

            // Display each entry in a simple panel
            foreach (var entry in combinedHistory)
            {
                // Add spacing between entries for readability
                AnsiConsole.WriteLine();

                var panel = new Panel(EscapeMarkup(entry))
                    .Border(BoxBorder.Rounded)
                    .BorderColor(Color.Green)
                    .Expand();

                AnsiConsole.Write(panel);
            }

            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("\nPress any key to return...");
            Console.ReadKey(true);
        }

        // Escape text safely for Spectre.Console
        private string EscapeMarkup(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            try
            {
                return Markup.Escape(text);
            }
            catch
            {
                return text.Replace("[", "(").Replace("]", ")");
            }
        }
        public void ShowHeaderSectionAdmin(string figletTitle, string ruleTitle, string rulemessage, Color panelColor, Color ruletitle, Color messagecolor)
        {
            AnsiConsole.Clear();
            var title = new FigletText(figletTitle)
                .Centered()
                .Color(panelColor);
            AnsiConsole.Write(title);
            AnsiConsole.WriteLine();

            AnsiConsole.Write(
               new Rule($"[bold {ruletitle}]{ruleTitle}[/]")
                   .RuleStyle(Style.Parse(ruletitle.ToString()))
                   .Centered());
            AnsiConsole.WriteLine();

            AnsiConsole.Write(new Markup($"[italic {messagecolor}]{rulemessage}[/]"));
            AnsiConsole.WriteLine();
            AnsiConsole.WriteLine();
        }
    }
}