using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCompare
{
    public class CarSearch
    {
        // Private fields for logged-in user and user store reference
        private readonly string? _loggedInUser;
        private readonly object _userStore;

        // Constructor with parameters
        public CarSearch(string? loggedInUser, object userStore)
        {
            _loggedInUser = loggedInUser;
            _userStore = userStore;
        }

        // Method to display car information in a table
        private void DisplayCarTable(Car car)
        {
            var rule = new Rule("📋 [bold underline cyan]Car Information[/]");
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .Centered();

            table.AddColumn("[bold]🔧 Property[/]");
            table.AddColumn("[bold]📊 Value[/]");

            table.AddRow("🔢 Registration", car.RegNumber);
            table.AddRow("🚗 Make", car.Brand);
            table.AddRow("📍 Model", car.Model);
            table.AddRow("📅 Year", car.Year.ToString());
            table.AddRow("🛣️ Mileage", $"[blue]{car.Mileage} km[/]");
            table.AddRow("👥 Owners", $"[grey]{car.Owners}[/]");
            table.AddRow("🛡️ Insurance Claims", $"[grey]{car.InsuranceClaims}[/]");
            table.AddRow("🧨 Known Issues", car.KnownIssues.Any() ? string.Join(", ", car.KnownIssues) : "[green]None[/]");

            string recommendationText = car.Recommendation switch
            {
                Recommendation.RiskyPurchase => "❌ [red]Risky Purchase[/]",
                Recommendation.Acceptable => "🟡 [yellow]Acceptable[/]",
                Recommendation.GoodInvestment => "✅ [green]Good Investment[/]",
                _ => "[grey]Unknown[/]"
            };
            table.AddRow("🧠 Recommendation", recommendationText);
            table.AddRow("📆 Car Age", $"{DateTime.Now.Year - car.Year} years");

            AnsiConsole.Write(table);
        }
        // Method to generate dummy car data for testing
        public Car GetDummyData(string regNumber)
        {
            Random random = new Random();

            string[] brand = { "Toyota", "Ford", "BMW", "Audi", "Honda", "Tesla", "Chevrolet", "Nissan" };
            string[] models = { "Model A", "Model B", "Model C", "Model D", "Model E" };
            string[] possibleIssues = { "Brake wear", "Oil leak", "Suspension noise", "Battery issue", "Transmission slip" };

            int year = random.Next(2000, 2023);
            DateTime carAge = new DateTime(year, 1, 1);

            int issueCount = random.Next(0, 4);
            var issues = possibleIssues.OrderBy(x => random.Next()).Take(issueCount).ToList();

            return new Car(
                regNumber: regNumber,
                brand: brand[random.Next(brand.Length)],
                model: models[random.Next(models.Length)],
                year: year,
                mileage: random.Next(0, 400000),
                owners: random.Next(1, 10),
                insuranceClaims: random.Next(0, 5),
                knownIssues: issues,
                carAge: carAge
            );
        }
        // Method to evaluate a car
        public void EvaluateCar(Car car)
        {
            int carAge = DateTime.Now.Year - car.Year;
            string message;
            Color panelColor;

            if (car.Mileage > 200000 && carAge > 12)
            {
                car.Recommendation = Recommendation.RiskyPurchase;
                message = "[red]Risky Purchase:[/] High mileage, several known issues, many previous owners, and an older vehicle age.";
                panelColor = Color.Red;
            }
            else if (car.Mileage < 100000 && carAge < 5)
            {
                car.Recommendation = Recommendation.GoodInvestment;
                message = "[green]Good Investment:[/] Low mileage, minimal issues, few previous owners, and a relatively new model.";
                panelColor = Color.Green;
            }
            else
            {
                car.Recommendation = Recommendation.Acceptable;
                message = "[yellow]Acceptable:[/] Moderate mileage, some known issues, average ownership history, or mid-range vehicle age.";
                panelColor = Color.Yellow;
            }

            var panel = new Panel(message)
                .Header("Car Evaluation", Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(panelColor))
                .Padding(1, 1, 1, 1)
                .Expand();

            AnsiConsole.Write(panel);
        }
        // Method to search by registration number
        public void SearchByRegNumber()
        {
            // Get user store and user ONCE before the loop
            var userStore = _userStore as DataStore<User>;
            var user = userStore?.List.FirstOrDefault(u => u.Username == _loggedInUser);

            // Check if user is logged in at the start
            bool isLoggedIn = user != null && userStore != null;
            if (!isLoggedIn)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ Warning:[/] You are not logged in. Search history will not be saved.");
                AnsiConsole.WriteLine();
                if (!AnsiConsole.Confirm("Do you want to continue without saving history?"))
                {
                    return;
                }
            }

            bool continueRunning = true;
            while (continueRunning)
            {
                try
                {
                    Console.Clear();
                    RenderSectionLayout(
                        figletTitle: "🚘 Car Evaluation 🚘",
                        panelMessage: "[white]You can search for a car by its registration number to get an evaluation based on various factors such as mileage, age, and known issues.[/]",
                        panelHeader: "🔍 [bold white]Car Search Information[/]",
                        ruleTitle: "📊 [bold cyan]Evaluation Process[/]"
                    );

                    string regNumber = AnsiConsole.Ask<string>("🔎 Enter the [green blink]registration number[/] of the car to evaluate:");
                    AnsiConsole.WriteLine();

                    // Using dummy data for demonstration and evaluation
                    Car car = GetDummyData(regNumber);
                    EvaluateCar(car);
                    AnsiConsole.WriteLine();

                    // Display car info
                    DisplayCarTable(car);
                    AnsiConsole.WriteLine();

                    // Save to search history ONLY if user is logged in
                    if (isLoggedIn)
                    {
                        user.SearchHistory ??= new List<Car>();   // ensure list exists
                        user.SearchHistory.Add(car);              // add the car object
                        userStore.SaveToJson();
                        AnsiConsole.MarkupLine("✅ [green]Search saved to history.[/]");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠️ Search not saved (not logged in).[/]");
                    }

                    AnsiConsole.WriteLine();

                    // Ask if user wants to evaluate another car
                    continueRunning = AnsiConsole.Confirm("Do you want to evaluate another car?");
                    if (!continueRunning)
                    {
                        AnsiConsole.WriteLine();
                        AnsiConsole.MarkupLine("🔙 Returning to the previous menu...");
                        Console.ReadLine();
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"❌ [red]Error:[/] An unexpected error occurred: {ex.Message}");
                    PauseReturn();
                    continueRunning = false;
                }
            }
        }
        // Method to show all searched cars from user.json
        public void ShowSearchHistory(string username)
        {
            Console.Clear();
            RenderSectionLayout(
                figletTitle: "📄 Search History 📄",
                panelMessage: "[white]View your previous car search history stored in your account.[/]",
                panelHeader: "🔍 [bold white]Search History Information[/]",
                ruleTitle: $"📋 [bold white]{username}'s Search History[/]"
            );

            if (string.IsNullOrWhiteSpace(username))
            {
                AnsiConsole.MarkupLine("❌ [red]Error:[/] No user is currently logged in.");
                PauseReturn();
                return;
            }

            if (_userStore == null)
            {
                AnsiConsole.MarkupLine("❌ [red]Error:[/] User store is not initialized.");
                PauseReturn();
                return;
            }

            var userStore = _userStore as DataStore<User>;
            if (userStore == null)
            {
                AnsiConsole.MarkupLine("❌ [red]Error:[/] Invalid user store type.");
                PauseReturn();
                return;
            }

            var user = userStore.List.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                AnsiConsole.MarkupLine($"❌ [red]Error:[/] User '[bold]{username}[/]' not found.");
                PauseReturn();
                return;
            }

            if (user.SearchHistory == null || user.SearchHistory.Count == 0)
            {
                AnsiConsole.MarkupLine("📭 [yellow]No search history found.[/]");
                PauseReturn();
                return;
            }

            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .Centered()
                .Expand();

            table.AddColumn(new TableColumn("[bold]🔢 Registration Number[/]").Centered());
            table.AddColumn(new TableColumn("[bold]📅 Search Date[/]").Centered());
            table.AddColumn(new TableColumn("[bold]🧠 Risk Level[/]").Centered());

            foreach (var car in user.SearchHistory)
            {
                if (car == null || string.IsNullOrWhiteSpace(car.RegNumber))
                    continue;

                string reg = car.RegNumber;
                string date = user.RegisteredAt.ToString("yyyy-MM-dd");

                Recommendation rec = Enum.IsDefined(typeof(Recommendation), car.Recommendation)
                    ? car.Recommendation
                    : Recommendation.Acceptable;

                string riskLevel = rec switch
                {
                    Recommendation.RiskyPurchase => "❌ [red]Risky Purchase[/]",
                    Recommendation.Acceptable => "🟡 [yellow]Acceptable[/]",
                    Recommendation.GoodInvestment => "✅ [green]Good Investment[/]",
                    _ => "❔ [grey]Unknown[/]"
                };

                table.AddRow($"[blue]{reg}[/]", date, riskLevel);
            }

            AnsiConsole.Write(table);
            PauseReturn();
        }
        // Method to clear search history for a user
        public void ClearSearchHistory(string username)
        {
            Console.Clear();
            RenderSectionLayout(
                figletTitle: "🧹 Clear Search History 🧹",
                panelMessage: "[white]Permanently delete your car search history from your account. This action cannot be undone.[/]",
                panelHeader: "🧹 [bold white]Clear Search History Information[/]",
                ruleTitle: "🗑️ [bold red]Clear Your Search History[/]"
            );

            if (string.IsNullOrWhiteSpace(username))
            {
                AnsiConsole.MarkupLine("❌ [red]Error:[/] No user is currently logged in.");
                PauseReturn();
                return;
            }

            var userStore = _userStore as DataStore<User>;
            var user = userStore?.List.FirstOrDefault(u => u.Username == username);

            if (user == null)
            {
                AnsiConsole.MarkupLine($"❌ [red]Error:[/] User '{username}' not found.");
                PauseReturn();
                return;
            }

            if (user.SearchHistory == null || !user.SearchHistory.Any())
            {
                AnsiConsole.MarkupLine("📭 [yellow]Your search history is already empty.[/]");
                PauseReturn();
                return;
            }

            AnsiConsole.MarkupLine("📋 [blue]Current Search History:[/]");
            foreach (var car in user.SearchHistory)
            {
                AnsiConsole.MarkupLine($"- [green]{car.RegNumber}[/]");
            }
            AnsiConsole.WriteLine();

            if (!AnsiConsole.Confirm("Are you sure you want to [red blink]clear[/] your search history?"))
            {
                AnsiConsole.MarkupLine("⚠️ [yellow]Search history clear cancelled.[/]");
                PauseReturn();
                return;
            }

            // Clear history and save to user.json
            user.SearchHistory.Clear();
            userStore.SaveToJson();

            AnsiConsole.MarkupLine("🧹 [green]Search history cleared successfully.[/]");
            PauseReturn();
        }
        // Method to pause and wait for user input before returning to menu
        private void PauseReturn()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("🔙 Press [green]⏎ Enter[/] to return to the menu...");
            Console.ReadLine();
        }

        // Method to render layout for sections with figlet header, panels and rules
        public void RenderSectionLayout(
            string figletTitle,
            string panelMessage,
            string panelHeader,
            string ruleTitle)
        {
            // Figlet header
            var header = new FigletText(figletTitle)
                .Centered()
                .Color(Color.White);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();

            // Login status panel
            var panelText = _loggedInUser != null
                ? $"[green]Logged in as:[/] [bold]{_loggedInUser}[/]"
                : "[red]Not logged in[/]";
            var panel = new Panel(panelText)
                .Header("👤 [bold white]User Status[/]", Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Blue)
                .Padding(1, 1);

            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();

            // Info panel
            var infoPanel = new Panel($"[green]{panelMessage}[/]")
                .Header(panelHeader)
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(Color.Aqua))
                .Padding(1, 1, 1, 1)
                .Expand();
            AnsiConsole.WriteLine();
            AnsiConsole.Write(infoPanel);
            AnsiConsole.WriteLine();

            // Section rule
            var rule = new Rule(ruleTitle);
            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();
        }
    }
}