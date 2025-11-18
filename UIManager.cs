using Microsoft.Extensions.Logging;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoCompare
{
    public class UIManager
    {
        private Admin _admin; // Remove 'readonly' keyword
        private readonly Logger _logger = new Logger("logs.txt");
        private readonly DataStore<User> _userStore = new DataStore<User>();
        private string? _loggedInUser;

        public void Start()
        {
            _userStore.LoadFromJson("users.json");
            _admin = new Admin(_userStore, _logger);

            while (true)
            {
                AnsiConsole.Clear();
                var title = new FigletText("AutoCompare")
                    .Color(Color.Green)
                    .Centered();
                AnsiConsole.Write(title);
                AnsiConsole.WriteLine();

                if (_loggedInUser == null)
                {
                    // Om användaren inte är inloggad
                    var menu = new SelectionPrompt<string>()
                        .Title("[yellow]Select an option:[/]")
                        .AddChoices("📝 Register", "🔐 Login", "❌ Exit");

                    var choice = AnsiConsole.Prompt(menu);

                    switch (choice)
                    {
                        case "📝 Register":
                            Register();
                            break;
                        case "🔐 Login":
                            Login();
                            break;
                        case "❌ Exit":
                            _userStore.SaveToJson("users.json");
                            return;
                    }
                }
                else
                {
                    // Huvudmeny när användaren är inloggad
                    var menu = new SelectionPrompt<string>()
                        .Title($"[yellow]Welcome {_loggedInUser}! Choose an option:[/]")
                        .AddChoices("🚗 Search Car", "📜 Manage Profile", "🚪 Logout");

                    var choice = AnsiConsole.Prompt(menu);

                    switch (choice)
                    {
                        case "🚗 Search Car":
                            SearchCarMenu();
                            break;
                        case "📜 Manage Profile":
                            ManageProfile();
                            break;
                        case "🚪 Logout":
                            Logout();
                            break;
                    }
                }
            }
        }

        private void SearchCarMenu()
        {

            var carSearch = new CarSearch();
            var user = _userStore.List.First(u => u.Username == _loggedInUser);

            bool running = true;
            while (running)
            {
                var menu = new SelectionPrompt<string>()
                    .Title("[yellow]Search Car Menu:[/]")
                    .AddChoices("🔍 Search by Registration Number", "📄 Show Search History", "🔙 Back");

                var choice = AnsiConsole.Prompt(menu);

                switch (choice)
                {
                    case "🔍 Search by Registration Number":
                        string reg = AnsiConsole.Ask<string>("Enter registration number:");
                        carSearch.SearchByRegNumber(reg);
                        user.SearchHistory.Add(reg);
                        _userStore.SaveToJson("users.json");
                        break;

                    case "📄 Show Search History":
                        if (user.SearchHistory.Count == 0)
                        {
                            AnsiConsole.MarkupLine("[grey]No previous searches.[/]");
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[green]Previous Searches:[/]");
                            foreach (var item in user.SearchHistory)
                                AnsiConsole.MarkupLine($"- {item}");
                        }
                        Pause();
                        break;

                    case "🔙 Back":
                        running = false;
                        break;
                }
            }
        }

        private void ManageProfile()
        {
            var user = _userStore.List.First(u => u.Username == _loggedInUser);
            var menu = new SelectionPrompt<string>()
                .Title("[yellow]Manage Profile:[/]")
                .AddChoices("🔑 Reset Password", "🗑 Delete Account", "🔙 Back");

            var choice = AnsiConsole.Prompt(menu);

            switch (choice)
            {
                case "🔑 Reset Password":
                    string newPassword = ReadHiddenPassword("Enter new password:");
                    if (user.ResetPassword(newPassword))
                        _userStore.SaveToJson("users.json");
                    Pause();
                    break;

                case "🗑 Delete Account":
                    if (AnsiConsole.Confirm($"Are you sure you want to delete account {_loggedInUser}?"))
                    {
                        user.DeleteAccount(_userStore);
                        _loggedInUser = null;
                        _userStore.SaveToJson("users.json");
                    }
                    Pause();
                    break;

                case "🔙 Back":
                    break;
            }
        }

        private void Register()
        {
            AnsiConsole.MarkupLine("[yellow]Registration[/]");
            var username = AnsiConsole.Ask<string>("Enter email:").Trim();

            if (_userStore.List.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine("[red]Email already registered![/]");
                Pause();
                return;
            }


            var password = ReadHiddenPassword("Enter password:");

            var method = AnsiConsole.Prompt(
                new SelectionPrompt<TwoFactorMethod>()
                    .Title("Choose 2FA method:")
                    .AddChoices(TwoFactorMethod.none, TwoFactorMethod.Email, TwoFactorMethod.SMS));

            string? contact = null;
            if (method == TwoFactorMethod.Email)
                contact = AnsiConsole.Ask<string>("Enter email:");
            else if (method == TwoFactorMethod.SMS)
                contact = AnsiConsole.Ask<string>("Enter phone number (with country code):");

            var tempUser = new User();
            if (!tempUser.Register(username, password, method, contact, _userStore))
            {
                AnsiConsole.MarkupLine("[red]Registration failed.[/]");
                Pause();
                return;
            }

            _userStore.AddItem(tempUser);
            _userStore.SaveToJson("users.json");
            AnsiConsole.MarkupLine($"[green]Account {username} registered![/]");
            Pause();
        }

        private void AdminPanel()
        {
            while (_admin.IsLoggedIn)
            {
                AnsiConsole.Clear();
                string choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[red]ADMIN PANEL[/]")
                        .AddChoices(
                            "Show All Users",
                            "Delete User",
                            "Show Log Files",
                            "Read Log File",
                            "Exit Admin"
                        ));

                switch (choice)
                {
                    case "Show All Users":
                        _admin.ShowAllUsers();
                        Pause();
                        break;

                    case "Delete User":
                        string userToDelete = AnsiConsole.Ask<string>("User to delete:");
                        _admin.DeleteUser(userToDelete);
                        Pause();
                        break;

                    case "Show Log Files":
                        _admin.ShowLogFiles();
                        Pause();
                        break;

                    case "Read Log File":
                        string file = AnsiConsole.Ask<string>("Enter log filename:");
                        _admin.ShowLogEntries(file);
                        Pause();
                        break;

                    case "Exit Admin":
                        return;
                }
            }
        }

        private void Login()
        {
            AnsiConsole.MarkupLine("[yellow]Login[/]");
            var username = AnsiConsole.Ask<string>("Enter email:").Trim();
            var password = ReadHiddenPassword("Enter password:").Trim();
           
            //  HIDDEN ADMIN LOGIN            
            if (_admin.TryLogin(username, password))
            {
                AdminPanel();
                return;
            }

            var user = _userStore.List.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null || !user.CheckPassword(password))
            {
                AnsiConsole.MarkupLine("[red]Wrong email or password.[/]");
                Pause();
                return;
            }
            bool verified = TwoFactor.Verify((global::TwoFactorMethod)(int)user.TwoFactorChoice, user.Email, user.PhoneNumber);

            if (!verified)
            {
                AnsiConsole.MarkupLine("[red]Login failed due to 2FA.[/]");
                Pause();
                return;
            }



            _loggedInUser = user.Username;
            AnsiConsole.MarkupLine($"[green]Welcome back, {user.Username}![/]");
            Pause();
        }

        private void Logout()
        {
            if (_loggedInUser != null)
            {
                AnsiConsole.MarkupLine($"[grey]{_loggedInUser} logged out.[/]");
                _loggedInUser = null;
            }
            Pause();
        }

        private string ReadHiddenPassword(string prompt)
        {
            AnsiConsole.MarkupLine($"[grey]{prompt}[/]");
            var password = string.Empty;

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    if (password.Length > 0)
                    {
                        password = password[..^1];
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    password += keyInfo.KeyChar;
                    Console.Write("*");
                }
            }

            return password.Trim();
        }

        private void Pause()
        {
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey(true);
        }
    }
}