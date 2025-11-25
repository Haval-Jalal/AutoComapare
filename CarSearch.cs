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
            var random = new Random();
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

        public void EvaluateCar(Car car)
        {
            try
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
            catch (Exception ex)
            {
                Logger.Log("EvaluateCar", ex);
                AnsiConsole.MarkupLine("An error occurred during car evaluation.");
            }
        }
        // Method to search by registration number
        public void SearchByRegNumber()
        {
            var userStore = _userStore as DataStore<User>;
            var user = userStore?.List.FirstOrDefault(u => u.Username == _loggedInUser);
            bool isLoggedIn = user != null && userStore != null;

            if (!isLoggedIn)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ Warning:[/] You are not logged in. Search history will not be saved.");
                AnsiConsole.WriteLine();
                if (!AnsiConsole.Confirm("Do you want to continue without saving history?")) return;
            }

            bool continueRunning = true;
            while (continueRunning)
            {
                try
                {
                    Console.Clear();
                    RenderSectionLayout("🚘 Car Evaluation 🚘", "[white]You can search for a car by its registration number to get an evaluation based on various factors such as mileage, age, and known issues.[/]", "🔍 [bold white]Car Search Information[/]", "📊 [bold cyan]Evaluation Process[/]");

                    string regNumber = AnsiConsole.Ask<string>("🔎 Enter the [green blink]registration number[/] of the car to evaluate:");
                    AnsiConsole.WriteLine();

                    Car car = GetDummyData(regNumber);
                    EvaluateCar(car);
                    AnsiConsole.WriteLine();
                    DisplayCarTable(car);
                    AnsiConsole.WriteLine();

                    if (isLoggedIn)
                    {
                        try
                        {
                            var carSearchStore = new DataStore<UserSearchHistory>("carsearchs.json");
                            carSearchStore.LoadFromJson();
                            var userSearch = carSearchStore.List.FirstOrDefault(u => u.Username == _loggedInUser);

                            if (userSearch == null)
                            {
                                userSearch = new UserSearchHistory { Username = _loggedInUser!, SearchHistory = new List<string>() };
                                carSearchStore.List.Add(userSearch);
                            }
                            userSearch.SearchHistory.Add(regNumber);
                            carSearchStore.SaveToJson();
                            AnsiConsole.MarkupLine("[green]✅ Search saved to your history.[/]");
                        }
                        catch (Exception ex) { AnsiConsole.MarkupLine($"[red]❌ Failed to save search: {ex.Message}[/]"); }
                    }
                    else AnsiConsole.MarkupLine("[yellow]⚠️ Search not saved (not logged in).[/]");

                    var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Would you like to [green]evaluate another car[/] or [red]return to the menu[/]?").AddChoices("🔄 Evaluate Another Car", "🔙 Return to Menu"));
                    if (choice == "🔙 Return to Menu") continueRunning = false;
                    else { Console.Clear(); SearchByRegNumber(); }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"❌ [red]Error:[/] An unexpected error occurred: {ex.Message}");
                    PauseReturn();
                    continueRunning = false;
                }
            }
        }

        public void ShowSearchHistory(string username)
        {
            Console.Clear();
            RenderSectionLayout("📄 Search History 📄", "[white]View your previous car search history stored in your account.[/]", "🔍 [bold white]Search History Information[/]", $"📋 [bold white]{username}'s Search History[/]");

            if (string.IsNullOrWhiteSpace(username)) { AnsiConsole.MarkupLine("❌ [red]Error:[/] No user is currently logged in."); PauseReturn(); return; }

            var carsearchsStore = new DataStore<UserSearchHistory>("carsearchs.json");
            carsearchsStore.LoadFromJson();
            var userSearch = carsearchsStore.List.FirstOrDefault(u => u.Username == username);

            if (userSearch == null || userSearch.SearchHistory == null || !userSearch.SearchHistory.Any())
            { AnsiConsole.MarkupLine("📭 [yellow]You have no search history.[/]"); PauseReturn(); return; }

            AnsiConsole.MarkupLine("🕘 [bold green]Your Previous Searches:[/]");
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Select a [blue]registration number[/] to view details or [red]Back[/]:").AddChoices(userSearch.SearchHistory.Append("🔙 [red]Back[/]")));
            if (choice == "🔙 [red]Back[/]") { PauseReturn(); return; }

            Car car = GetDummyData(choice);
            EvaluateCar(car);
            AnsiConsole.WriteLine();
            DisplayCarTable(car);
            AnsiConsole.MarkupLine("🔙 Press [green]⏎ Enter[/] to return to your search history...");
            Console.ReadLine();
            ShowSearchHistory(username);
        }

        public void ClearSearchHistory(string username)
        {
            Console.Clear();
            RenderSectionLayout("🧹 Clear History 🧹", "[white]Permanently delete your car search history from your account. This action cannot be undone.[/]", "🧹 [bold white]Clear Search History Information[/]", "🗑️ [bold red]Clear Your Search History[/]");

            if (string.IsNullOrWhiteSpace(username)) { AnsiConsole.MarkupLine("❌ [red]Error:[/] No user is currently logged in."); PauseReturn(); return; }

            var carsearchsStore = new DataStore<UserSearchHistory>("carsearchs.json");
            carsearchsStore.LoadFromJson();
            var userSearch = carsearchsStore.List.FirstOrDefault(u => u.Username == username);

            if (userSearch == null || userSearch.SearchHistory == null || !userSearch.SearchHistory.Any())
            { AnsiConsole.MarkupLine("📭 [yellow]You have no search history to clear.[/]"); PauseReturn(); return; }

            foreach (var item in userSearch.SearchHistory) AnsiConsole.MarkupLine($"- [red blink]{item}[/]");

            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Are you sure you want to [red]clear your search history[/]? This action cannot be undone.").AddChoices("✅ Yes, Clear History", "❌ No, Go Back"));

            if (choice == "✅ Yes, Clear History")
            {
                userSearch.SearchHistory.Clear();
                carsearchsStore.SaveToJson();
                AnsiConsole.MarkupLine("✅ [green]Your search history has been cleared.[/]");
            }
            else AnsiConsole.MarkupLine("ℹ️ [yellow]No changes made to your search history.[/]");

            PauseReturn();
        }

        private void PauseReturn()
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("🔙 Press [green]⏎ Enter[/] to return to the menu...");
            Console.ReadLine();
        }

        public void RenderSectionLayout(string figletTitle, string panelMessage, string panelHeader, string ruleTitle)
        {
            AnsiConsole.Write(new FigletText(figletTitle).Centered().Color(Color.White));
            AnsiConsole.WriteLine();

            var loginStatus = string.IsNullOrWhiteSpace(_loggedInUser) ? "[red]Not Logged In[/]" : $"[green]Logged in as:[/] [bold]{_loggedInUser}[/]";
            AnsiConsole.Write(new Rule(loginStatus));
            AnsiConsole.WriteLine();

            AnsiConsole.WriteLine();
            AnsiConsole.Write(new Panel($"[green]{panelMessage}[/]").Header(panelHeader).Border(BoxBorder.Rounded).BorderStyle(new Style(Color.Aqua)).Padding(1, 1, 1, 1).Expand());
            AnsiConsole.WriteLine();

            AnsiConsole.Write(new Rule(ruleTitle));
            AnsiConsole.WriteLine();
        }
    }
}