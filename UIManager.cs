using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoCompare
{
    public class UIManager
    {
        private readonly DataStore<User> _userStore = new DataStore<User>();
        private string? _loggedInUser;

        public void Start()
        {
            _userStore.LoadFromJson("users.json");

            while (true)
            {
                AnsiConsole.Clear();
                var title = new FigletText("AutoCompare")
                    .Color(Color.Green)
                    .Centered();
                AnsiConsole.Write(title);
                AnsiConsole.WriteLine();

                var menu = new SelectionPrompt<string>()
                    .Title("[yellow]Select an option:[/]")
                    .AddChoices(_loggedInUser == null
                        ? new[] { "📝 Register", "🔐 Login", "❌ Exit" }
                        : new[] { "📜 Profile", "🚪 Logout", "❌ Exit" });

                var choice = AnsiConsole.Prompt(menu);

                switch (choice)
                {
                    case "📝 Register":
                        Register();
                        break;
                    case "🔐 Login":
                        Login();
                        break;
                    case "📜 Profile":
                        ShowProfile();
                        break;
                    case "🚪 Logout":
                        Logout();
                        break;
                    case "❌ Exit":
                        _userStore.SaveToJson("users.json");
                        return;
                }
            }
        }

        private void Register()
        {
            AnsiConsole.MarkupLine("[yellow]Registration[/]");
            var username = AnsiConsole.Ask<string>("Enter username:").Trim();

            if (_userStore.List.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine("[red]Username already taken.[/]");
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

        private void Login()
        {
            AnsiConsole.MarkupLine("[yellow]Login[/]");
            var username = AnsiConsole.Ask<string>("Enter username:").Trim();
            var password = ReadHiddenPassword("Enter password:").Trim();

            var user = _userStore.List.FirstOrDefault(u =>
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null || !user.CheckPassword(password))
            {
                AnsiConsole.MarkupLine("[red]Wrong username or password.[/]");
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

        private void ShowProfile()
        {
            if (_loggedInUser == null)
            {
                AnsiConsole.MarkupLine("[red]You are not logged in.[/]");
                Pause();
                return;
            }

            AnsiConsole.Write(new Rule($"[bold yellow]{_loggedInUser}'s profile[/]").RuleStyle("grey"));
            AnsiConsole.MarkupLine($"[green]Username:[/] {_loggedInUser}");
            AnsiConsole.MarkupLine("[grey](No additional info yet.)[/]");
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