using Spectre.Console;
using System;
using System.Linq;

namespace AutoCompare
{
    public class UIManager
    {
        // CHANGED: UIManager now owns all DataStores and the Logger
        private readonly DataStore<User> _userStore = new DataStore<User>("users.json");
        private readonly DataStore<Car> _carStore = new DataStore<Car>("cars.json");
        private readonly DataStore<CarSearch> _carSearchStore = new DataStore<CarSearch>("carsearchs.json");
        private readonly Logger _logger = new Logger("logs/logs.json");
        private readonly Admin _admin;
        private string? _loggedInUser;
        private readonly AIService _aiService = new AIService();

        // NEW: Background image URL placeholder (you can later use this to render ascii/coloured background)
        // NOTE: For console apps we cannot directly show an image, but we can convert to ASCII or use ANSI art.
        public string BackgroundImageUrl { get; set; } = "https://img.sm360.ca/ir/w640h390c/images/newcar/ca/2025/bmw/serie-8-coupe/m850i-xdrive/coupe/exteriorColors/2025_bmw_serie-8-coupe_ext_032_416.png"; // placeholder

        public UIManager()
        {
            _admin = new Admin(_userStore, _logger);
        }

        // Duplicate Start method removed; use the async Task Start() implementation defined below.

        // NEW: Centralized guest menu with arrow navigation and nicer layout
        private void ShowGuestMenu()
        {
            var menu = new SelectionPrompt<string>()
                .Title("[yellow]Select an option:[/]")
                .PageSize(10)
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
                    // CHANGED: Do not save on Exit; saves happen on mutation in DataStore
                    Environment.Exit(0);
                    break;
            }
        }

        private async Task ShowUserMenu()
        {
            var menu = new SelectionPrompt<string>()
                .Title($"[yellow]Welcome {_loggedInUser}! Choose an option:[/]")
                .PageSize(10)
                .AddChoices(
                    "🚗 Search Car",
                    "🤖 Ask AI about a Car Model",
                    "📄 Show AI Search History", // ny val
                    "📜 Manage Profile",
                    "🛠 Admin Panel",
                    "🚪 Logout"
                );

            var choice = AnsiConsole.Prompt(menu);

            switch (choice)
            {
                case "🚗 Search Car":
                    SearchCarMenu();
                    break;
                case "🤖 Ask AI about a Car Model":
                    await AskAiFlow();
                    break;
                case "📄 Show AI Search History":
                    ShowAiSearchHistory();
                    break;
                case "📜 Manage Profile":
                    ManageProfile();
                    break;
                case "🛠 Admin Panel":
                    if (_admin.TryLoginPrompt())
                        AdminPanel();
                    break;
                case "🚪 Logout":
                    Logout();
                    break;
            }
        }

        // CHANGED: Register now uses the shared _userStore and DOES NOT call LoadFromJson
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
            if (!tempUser.Register(username, password, method, contact))
            {
                AnsiConsole.MarkupLine("[red]Registration failed.[/]");
                Pause();
                return;
            }

            // CHANGED: Save by adding to the UIManager-owned _userStore (this will call SaveToJson automatically)
            _userStore.AddItem(tempUser);
            AnsiConsole.MarkupLine($"[green]Account {username} registered![/]");
            Pause();
        }

        private void AdminPanel()
        {
            while (_admin.IsLoggedIn)
            {
                AnsiConsole.Clear();
                var options = new SelectionPrompt<string>()
                    .Title("[red]ADMIN PANEL[/]")
                    .AddChoices("Show All Users", "Delete User", "Show Log Files", "Read Log File", "Exit Admin");

                var choice = AnsiConsole.Prompt(options);
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

        // CHANGED: Login uses _userStore (shared)
        private void Login()
        {
            AnsiConsole.MarkupLine("[yellow]Login[/]");
            var username = AnsiConsole.Ask<string>("Enter email:").Trim();
            var password = ReadHiddenPassword("Enter password:").Trim();

            // Admin login check
            if (_admin.TryLogin(username, password))
            {
                AdminPanel();
                return;
            }

            var user = _userStore.List.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (user == null || !user.CheckPassword(password))
            {
                AnsiConsole.MarkupLine("[red]Wrong email or password.[/]");
                Pause();
                return;
            }

            bool verified = TwoFactor.Verify(user.TwoFactorChoice, user.Email, user.PhoneNumber);
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
                        carSearch.SearchByRegNumberInteractive(reg);
                        user.SearchHistory.Add(reg);
                        _userStore.SaveToJson(); // persist search history change immediately
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
                    {
                        // DataStore won't detect property mutation automatically, so we Save explicitly
                        _userStore.SaveToJson(); // CHANGED: Save after mutating user object
                        AnsiConsole.MarkupLine("[green]Password updated[/]");
                    }
                    Pause();
                    break;

                case "🗑 Delete Account":
                    if (AnsiConsole.Confirm($"Are you sure you want to delete account {_loggedInUser}?"))
                    {
                        bool removed = user.DeleteAccount(_userStore); // DeleteAccount uses userStore.RemoveItem
                        if (removed)
                        {
                            _loggedInUser = null;
                        }
                    }
                    Pause();
                    break;

                case "🔙 Back":
                    break;
            }
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

        public void ShowIntroAnimation()
        {
            AnsiConsole.Clear();

            string car = "🚗💨";
            string text = "AutoCompare";
            int width = Console.WindowWidth;
            int start = -20;
            int end = width - text.Length - 5;

            for (int pos = start; pos < end; pos++)
            {
                AnsiConsole.Clear();

                string spacing = new string(' ', Math.Max(pos, 0));

                // Draw car + text
                AnsiConsole.MarkupLine($"[green]{spacing}{car} {text}[/]");

                Thread.Sleep(20);
            }

            Thread.Sleep(500);
            AnsiConsole.Clear();
        }

        //AI 
        private async Task AskAiFlow()
        {
            AnsiConsole.MarkupLine("[cyan]Ask AI about a Car Model[/]");

            string question = AnsiConsole.Ask<string>("Enter car model or question:");

            // Optional: top 3 relevant cars as context
            var contextCars = _carStore.List
                .Where(c => c.Model.Contains(question, StringComparison.OrdinalIgnoreCase)
                        || c.Brand.Contains(question, StringComparison.OrdinalIgnoreCase))
                .Take(3)
                .ToList();

            try
            {
                AiResult result = await _aiService.AskCarModelAsync(question, contextCars);

                var panel = new Panel($"[bold]Answer:[/]\n{result.Answer}\n\n[bold]Summary:[/]\n{result.Summary}")
                    .Header("AI Car Info")
                    .Border(BoxBorder.Rounded)
                    .Padding(1, 1, 1, 1)
                    .Expand();

                AnsiConsole.Write(panel);

                // Save AI query to user's search history
                var user = _userStore.List.First(u => u.Username == _loggedInUser);
                string entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} | Q: {question} | Summary: {result.Summary}";
                user.SearchHistory.Add(entry);
                _userStore.SaveToJson();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error contacting AI: {ex.Message}[/]");
            }

            Pause();
        }

        // Show previous AI queries
        private void ShowAiSearchHistory()
        {
            var user = _userStore.List.First(u => u.Username == _loggedInUser);

            var aiHistory = user.SearchHistory
                .Where(e => e.Contains("Summary:")) // filter AI entries
                .ToList();

            if (aiHistory.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No AI searches yet.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[green]AI Search History:[/]");
                foreach (var item in aiHistory)
                    AnsiConsole.MarkupLine($"- {item}");
            }

            Pause();
        }

        public async Task Start()
        {
            _userStore.LoadFromJson();
            _carStore.LoadFromJson();
            _carSearchStore.LoadFromJson();

            ShowIntroAnimation();

            while (true)
            {
                AnsiConsole.Clear();

                var title = new FigletText("AutoCompare")
                    .Centered()
                    .Color(Color.Green);
                AnsiConsole.Write(title);
                AnsiConsole.WriteLine();

                if (_loggedInUser == null)
                {
                    ShowGuestMenu();
                }
                else
                {
                    await ShowUserMenu();
                }

                var centeredText = new Panel("[yellow]Select an option:[/]")
                    .Border(BoxBorder.None)
                    .Expand()
                    .Padding(1, 1, 1, 1);

                AnsiConsole.Write(centeredText);
            }
        }
    }
}