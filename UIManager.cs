using Spectre.Console;
using System;
using System.Linq;
using System.Text;

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
                    "📜 Manage Profile",
                    "ℹ️ About us",
                    "🚪 Logout"
                );

            var choice = AnsiConsole.Prompt(menu);

            switch (choice)
            {
                case "🚗 Search Car":
                    SearchCarMenu();
                    break;
                case "🤖 Ask AI about a Car Model":
                    await AskAiChatLoop();
                    break;
                case "📜 Manage Profile":
                    ManageProfile();
                    break;
                case "ℹ️ About us":
                    ShowAbout();
                    break;
                case "🚪 Logout":
                    Logout();
                    break;
            }
        }

        // CHANGED: Register now uses the shared _userStore and DOES NOT call LoadFromJson
        private void Register()
        {
            AnsiConsole.MarkupLine("[green]──────────────────────────────────────────────────────────[/]");
            AnsiConsole.MarkupLine("[bold green]📝 Registration[/]");
            AnsiConsole.MarkupLine("[green]──────────────────────────────────────────────────────────[/]\n");

            var username = AnsiConsole.Ask<string>(
                "[yellow]Enter email[/] [grey](type 'exit' to go back)[/]:").Trim();

            if (username.Equals("exit", StringComparison.OrdinalIgnoreCase))
                return;

            if (_userStore.List.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                AnsiConsole.MarkupLine("[red]❌ Email already registered![/]");
                Pause();
                return;
            }

            var password = ReadHiddenPassword("Enter password:");

            var method = AnsiConsole.Prompt(
                new SelectionPrompt<TwoFactorMethod>()
                    .Title("[yellow]Choose 2FA method:[/]")
                    .AddChoices(TwoFactorMethod.none, TwoFactorMethod.Email, TwoFactorMethod.SMS));

            string? contact = null;

            if (method == TwoFactorMethod.Email)
            {
                contact = AnsiConsole.Ask<string>("[cyan]Enter email for 2FA:[/]");
            }
            else if (method == TwoFactorMethod.SMS)
            {
                contact = AnsiConsole.Ask<string>("[cyan]Enter phone number (with country code):[/]");
            }

            var tempUser = new User();
            if (!tempUser.Register(username, password, method, contact))
            {
                AnsiConsole.MarkupLine("[red]❌ Registration failed.[/]");
                Pause();
                return;
            }

            _userStore.AddItem(tempUser);

            AnsiConsole.MarkupLine($"\n[green]✅ Account [bold]{username}[/] registered successfully![/]");
            Pause();
        }

        private void AdminPanel()
        {
            while (_admin.IsLoggedIn)
            {
                AnsiConsole.Clear();
                var options = new SelectionPrompt<string>()
                    .Title("[red]ADMIN PANEL[/]")
                    .AddChoices("Show All Users", "Show AI Search History", "Delete User", "Show Log Files", "Read Log File", "Exit Admin");

                var choice = AnsiConsole.Prompt(options);
                switch (choice)
                {
                    case "Show All Users":
                        _admin.ShowAllUsers();
                        Pause();
                        break;
                    case "Show AI Search History":
                        _admin.ShowAiSearchHistoryInteractive();
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
        private void ShowAbout()
        {
            AnsiConsole.MarkupLine("[green]AutoCompare[/]");
            AnsiConsole.MarkupLine("AutoCompare helps you further investigate and find information about cars. You provide a registration number - we provide the information.");
            AnsiConsole.MarkupLine("Our goal is to ensure our users makes a risk-free purchase by providing detailed information about the cars you're browsing.\n");

            //AnsiConsole.MarkupLine("[yellow]Press any key to go back to the main menu...[/]");
            //Console.ReadKey(true);
            // Back-knapp via SelectionPrompt
            var menu = new SelectionPrompt<string>()
                .Title("[yellow]Tryck 'Back' för att återgå till huvudmenyn[/]")
                .AddChoices("🔙 Back");

            AnsiConsole.Prompt(menu);

            // När användaren väljer "Back" returnerar metoden och användaren är tillbaka i ShowUserMenu
        }
        


        // CHANGED: Login uses _userStore (shared)
        private void Login()
{
    AnsiConsole.MarkupLine("[green]──────────────────────────────────────────────────────────[/]");
    AnsiConsole.MarkupLine("[bold green]🔐 Login[/]");
    AnsiConsole.MarkupLine("[green]──────────────────────────────────────────────────────────[/]\n");

    var username = AnsiConsole.Ask<string>(
        "[yellow]Enter email[/] [grey](type 'exit' to go back)[/]:").Trim();

    if (username.Equals("exit", StringComparison.OrdinalIgnoreCase))
        return;

    var password = ReadHiddenPassword("Enter password:").Trim();

    // Admin login check
    if (_admin.TryLogin(username, password))
    {
        AnsiConsole.MarkupLine($"\n[green]🛠️ Logged in as Admin![/]");
        Pause();
        AdminPanel();
        return;
    }

    var user = _userStore.List.FirstOrDefault(u =>
        u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

    if (user == null)
    {
        AnsiConsole.MarkupLine("[red]❌ No account found with that email.[/]");
        Pause();
        return;
    }

    if (!user.CheckPassword(password))
    {
        AnsiConsole.MarkupLine("[red]❌ Incorrect password.[/]");
        Pause();
        return;
    }

    // 2FA Verification
    bool verified = TwoFactor.Verify(user.TwoFactorChoice, user.Email, user.PhoneNumber);

    if (!verified)
    {
        AnsiConsole.MarkupLine("[red]❌ Login failed due to invalid 2FA code.[/]");
        Pause();
        return;
    }

    _loggedInUser = user.Username;

    AnsiConsole.MarkupLine($"\n[green]✅ Welcome back, [bold]{user.Username}[/]![/]");
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

        // AI Chat / Multi-turn Loop
        // AskAiChatLoop: ChatGPT-style multi-turn AI chat for cars with minimal emojis and clean output
        private async Task AskAiChatLoop()
        {
            AnsiConsole.MarkupLine("[cyan]🚗 AI Car Chat — ask about car models (type 'exit' to go back)[/]");
            var helper = new AiHelper(); // AiHelper reads OPENAI_API_KEY from env

            const string systemInstruction = 
                "You are an expert automotive assistant. Answer clearly and factually. " +
                "Use minimal headings, but emojis are allowed to mark pros, cons, and tips. " +
                "✅ = positive / advantage, ⚠️ = caution / drawback, 🛠️ = maintenance / tip. " +
                "When user asks follow-up questions, remember prior conversation context.";

            var convo = new List<(string role, string content)> { ("system", systemInstruction) };
            const int maxTurnsToKeep = 12;

            while (true)
            {
                string userInput = AnsiConsole.Ask<string>("You:").Trim();
                if (string.IsNullOrWhiteSpace(userInput)) continue;
                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                convo.Add(("user", userInput));

                // Keep recent context + system message
                if (convo.Count > maxTurnsToKeep)
                {
                    var sys = convo.First(t => t.role == "system");
                    var tail = convo.Where(t => t.role != "system").Skip(Math.Max(0, convo.Count - maxTurnsToKeep)).ToList();
                    convo = new List<(string role, string content)> { sys };
                    convo.AddRange(tail);
                }

                string assistantReply = string.Empty;

                try
                {
                    assistantReply = await helper.ChatMessagesAsync(convo, model: "gpt-4o-mini", maxTokens: 1200, temperature: 0.0);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]AI error:[/] {EscapeMarkup(ex.Message)}");
                    convo.RemoveAt(convo.Count - 1); // remove user message so not resent
                    continue;
                }

                convo.Add(("assistant", assistantReply));

                // Display assistant reply nicely
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[green]🤖 AI:[/]");
                foreach (var line in assistantReply.Split('\n'))
                {
                    string trimmed = line.Trim();

                    if (trimmed.StartsWith("Pros:", StringComparison.OrdinalIgnoreCase) ||
                        trimmed.StartsWith("✅"))
                    {
                        AnsiConsole.MarkupLine($"[green]{EscapeMarkup(trimmed)}[/]");
                    }
                    else if (trimmed.StartsWith("Cons:", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("⚠️"))
                    {
                        AnsiConsole.MarkupLine($"[yellow]{EscapeMarkup(trimmed)}[/]");
                    }
                    else if (trimmed.StartsWith("Tip:", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("🛠️"))
                    {
                        AnsiConsole.MarkupLine($"[cyan]{EscapeMarkup(trimmed)}[/]");
                    }
                    else
                    {
                        AnsiConsole.WriteLine(EscapeMarkup(trimmed));
                    }
                }
                AnsiConsole.WriteLine();

                // Follow-up or finish
                var next = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Choose next action:")
                        .AddChoices(new[] { "Ask follow-up", "Finish chat" })
                );

                if (next == "Finish chat") break;
            }

            // Save a short summary to user's search history
            try
            {
                var user = _userStore.List.FirstOrDefault(u => u.Username == _loggedInUser);
                if (user != null)
                {
                    var lastAssistant = convo.LastOrDefault(t => t.role == "assistant").content ?? string.Empty;
                    var summary = lastAssistant.Length > 200 ? lastAssistant.Substring(0, 197) + "..." : lastAssistant;
                    string entry = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm} | 🤖 {EscapeMarkup(summary)}";
                    user.SearchHistory ??= new List<string>();
                    user.SearchHistory.Add(entry);
                    _userStore.SaveToJson();
                }
            }
            catch
            {
                // don't block user if save fails
            }
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
    }
}    