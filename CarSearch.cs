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
        // Car class to hold car information and evaluation with user name result in json file 
        public class UserSearchHistory
        {
            public string Username { get; set; }
            public List<string> SearchHistory { get; set; } = new();
        }

        // Private fields for logged-in user and user store reference
        private readonly string? _loggedInUser;
        private readonly object _userStore;

        public CarSearch() { }

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
            var carsearchsStore = new DataStore<Car>("carsearchs.json");
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

                    // Save car to search history if user is logged in in carsearchs.json
                    if (isLoggedIn)
                    {
                        try
                        {
                            var carSearchStore = new DataStore<UserSearchHistory>("carsearchs.json");
                            carSearchStore.LoadFromJson();

                            var userSearch = carSearchStore.List
                                .FirstOrDefault(u => u.Username == _loggedInUser);

                            if (userSearch == null)
                            {
                                userSearch = new UserSearchHistory
                                {
                                    Username = _loggedInUser!,
                                    SearchHistory = new List<string>()
                                };
                                carSearchStore.List.Add(userSearch);
                            }
                            // Add the regNumber to history
                            userSearch.SearchHistory.Add(regNumber);

                            carSearchStore.SaveToJson();

                            AnsiConsole.MarkupLine("[green]✅ Search saved to your history.[/]");
                        }
                        catch (Exception ex)
                        {
                            AnsiConsole.MarkupLine($"[red]❌ Failed to save search: {ex.Message}[/]");
                        }
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]⚠️ Search not saved (not logged in).[/]");
                    }
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
            var carsearchsStore = new DataStore<UserSearchHistory>("carsearchs.json");
            carsearchsStore.LoadFromJson();
            var userSearch = carsearchsStore.List.FirstOrDefault(u => u.Username == username);
            if (userSearch == null || userSearch.SearchHistory == null || !userSearch.SearchHistory.Any())
            {
                AnsiConsole.MarkupLine("📭 [yellow]You have no search history.[/]");
                PauseReturn();
                return;
            }
            AnsiConsole.MarkupLine("🕘 [bold green]Your Previous Searches:[/]");
            var selectcar = new SelectionPrompt<string>()
             .Title("Select a [blue]registration number[/] to view details or [red]Back[/]:")
                .AddChoices(userSearch.SearchHistory.Append("🔙 [red]Back[/]"));
            var choice = AnsiConsole.Prompt(selectcar);
            if (choice == "🔙 [red]Back[/]")
            {
                PauseReturn();
                return;
            }
            // Display dummy car data for the selected registration number
            Car car = GetDummyData(choice);
            EvaluateCar(car);
            AnsiConsole.WriteLine();
            DisplayCarTable(car);
            AnsiConsole.MarkupLine("🔙 Press [green]⏎ Enter[/] to return to your search history...");
            Console.ReadLine();
            ShowSearchHistory(username); // Return to search history after viewing details
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
            var carsearchsStore = new DataStore<UserSearchHistory>("carsearchs.json");
            carsearchsStore.LoadFromJson();
            var userSearch = carsearchsStore.List.FirstOrDefault(u => u.Username == username);
            if (userSearch == null || userSearch.SearchHistory == null || !userSearch.SearchHistory.Any())
            {
                AnsiConsole.MarkupLine("📭 [yellow]You have no search history to clear.[/]");
                PauseReturn();
                return;
            }
            foreach (var item in userSearch.SearchHistory)
            {
                AnsiConsole.MarkupLine($"- [red blink]{item}[/]");
            }
            bool confirmClear = AnsiConsole.Confirm($"Are you sure you want to [red blink]clear[/] your search history, {username}? This action cannot be undone.");
            if (confirmClear)
            {
                userSearch.SearchHistory.Clear();
                carsearchsStore.SaveToJson();
                AnsiConsole.MarkupLine("✅ [green]Your search history has been cleared.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("❌ [yellow]Action cancelled. Your search history remains intact.[/]");
            }
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

            // Login status rule
            var loginStatus = string.IsNullOrWhiteSpace(_loggedInUser)
                ? "[red]Not Logged In[/]"
                : $"[green]Logged in as:[/] [bold]{_loggedInUser}[/]";
            var statusRule = new Rule(loginStatus);
            AnsiConsole.Write(statusRule);

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