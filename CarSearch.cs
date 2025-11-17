using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoCompare
{
    public class CarSearch
    {
        private readonly DataStore<Car> _carSearchStore = new DataStore<Car>();
        private readonly DataStore<User> _userStore = new DataStore<User>();

        //Metoder för att söka bil baserat på registreringsnummer
        public void SearchByRegNumber()
        {
            var selectionPrompt = new SelectionPrompt<string>()
                .Title("📂 [bold underline]Select an option[/]:")
                .AddChoices(new[]
                {
                    "🔎 Search by Registration Number",
                    "🔙 Back to Menu"
                });
            var selection = AnsiConsole.Prompt(selectionPrompt);

            if (selection == "🔙 Back to Menu")
            {
                return; // Return to menu
            }
            else if (selection == "🔎 Search by Registration Number")

            {
                bool continueRunning = true;
                while (continueRunning)
                {
                    Console.Clear();
                    var header = new FigletText("🚘 Car Evaluation System 🚘")
                        .Centered()
                        .Color(Color.DarkOliveGreen1_1);

                    AnsiConsole.Write(header);
                    AnsiConsole.WriteLine();

                    //UserName som hämtas från inloggad användare
                    string regNumber = AnsiConsole.Ask<string>("🔎 Enter the [green blink]registration number[/] of the car to evaluate:");
                    AnsiConsole.WriteLine();

                    Car car = GetDummyData(regNumber);
                    EvaluateCar(car);

                    User currentUser = _userStore.List.FirstOrDefault(); //Hämta inloggad användare (för demo, ta första användaren)
                    if (currentUser != null)
                    {
                        if (!currentUser.SearchHistory.Contains(car.RegNumber))
                        {
                            currentUser.SearchHistory.Add(car.RegNumber);
                            _userStore.UpdateItem(currentUser);
                            SaveUserSearchesToJson(currentUser); // see suggestion 3
                            AnsiConsole.MarkupLine("💾 [green]Saved car search to your profile[/]");
                            AnsiConsole.WriteLine();
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("📁 [yellow]This car is already in your search history.[/]");
                            AnsiConsole.WriteLine();
                        }
                    }
                    else
                    {
                        // user not logged in, skip saving
                        AnsiConsole.MarkupLine("\n[red]No logged-in user found. Car search will not be saved.[/]");
                        AnsiConsole.WriteLine();
                    }

                    // Display car info
                    DisplayCarTable(car);


                    // Save to global store
                    _carSearchStore.AddItem(car);
                    continueRunning = AnsiConsole.Confirm("\nDo you want to evaluate another car?");
                }
            }
        }
        //Metod för att visa bilinformation i en tabell
        private void DisplayCarTable(Car car)
        {
            var rule = new Rule("📋 [bold underline cyan]Car Information[/]");

            AnsiConsole.Write(rule);
            AnsiConsole.WriteLine();

            var table = new Table()
                //.Title("📋 [bold underline cyan]Car Information[/]")
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
        //Metod för att hämta dummydata för en bil
        public Car GetDummyData(string regNumber)
        {
            AnsiConsole.Status()
              .Spinner(Spinner.Known.Dots)
              .SpinnerStyle(Style.Parse("grey"))
              .Start($"⏳ Generating dummy data for [bold]{regNumber}[/]...", ctx =>
              {
                  Task.Delay(2000).Wait(); // Simulated work
              });


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
        //Metod för att utvärdera en bil
        public void EvaluateCar(Car car)
        {
            int carAge = DateTime.Now.Year - car.Year;
            string message;
            Color panelColor;

            if (car.Mileage > 200000 && carAge > 12)
            {
                car.Recommendation = Recommendation.RiskyPurchase;
                message = "❌ [red]Risky Purchase:[/] High mileage, several known issues, many previous owners, and an older vehicle age.";
                panelColor = Color.Red;
            }
            else if (car.Mileage < 100000 && carAge < 5)
            {
                car.Recommendation = Recommendation.GoodInvestment;
                message = "✅ [green]Good Investment:[/] Low mileage, minimal issues, few previous owners, and a relatively new model.";
                panelColor = Color.Green;
            }
            else
            {
                car.Recommendation = Recommendation.Acceptable;
                message = "🟡 [yellow]Acceptable:[/] Moderate mileage, some known issues, average ownership history, or mid-range vehicle age.";
                panelColor = Color.Yellow;
            }

            var panel = new Panel(message)
                .Header("🧠 [bold underline]Car Evaluation[/]", Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(panelColor))
                .Padding(1, 1, 1, 1);

            AnsiConsole.Write(panel);

        }
        //Metod för att spara bilinformation som JSON
        public void SaveDataAsJson(Car car)
        {
            string filePath = $"users.json";
            List<Car> carsSearch = new();

            //Om filen finns, läs in den befintliga listan
            if (File.Exists(filePath))
            {
                string existingJson = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(existingJson))
                {
                    carsSearch = JsonSerializer.Deserialize<List<Car>>(existingJson) ?? new List<Car>();
                }
            }

            //Lägg till den nya bilen i listan
            carsSearch.Add(car);

            //Spara hela listan 
            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(carsSearch, options);
            File.WriteAllText(filePath, updatedJson);
        }
        //Metod för att spara användarens sökhistorik till JSON
        public void SaveUserSearchesToJson(User user)
        {
            _userStore.SaveToJson("users.json"); // reuse DataStore.SaveToJson (ensure List was updated)
        }
        //Add new car from user input by using Car class and saving it to JSON file if inlogged
        public void AddNewCarFromUserInput()
        {
            var selectionPrompt = new SelectionPrompt<string>()
                .Title("📂 [bold underline]Select an option[/]:")
                .AddChoices(new[]
                {
                    "➕ Add New Car",
                    "🔙 Back to Menu"
                });
            var selection = AnsiConsole.Prompt(selectionPrompt);
            if (selection == "🔙 Back to Menu")
            {
                return; // Return to menu
            }
            else if (selection == "➕ Add New Car")
            {
                bool AddMoreCars = true;
                while (AddMoreCars)
                {
                    Console.Clear();
                    var header = new FigletText("🚘 Add New Car 🚘")
                        .Centered()
                        .Color(Color.DarkOliveGreen1_1);
                    AnsiConsole.Write(header);
                    AnsiConsole.WriteLine();
                    string regNumber = AnsiConsole.Ask<string>("Enter the [green blink]registration number[/]:");
                    string brand = AnsiConsole.Ask<string>("Enter the [green blink]brand[/]:");
                    string model = AnsiConsole.Ask<string>("Enter the [green blink]model[/]:");
                    int year = AnsiConsole.Ask<int>("Enter the [green blink]year[/]:");
                    int mileage = AnsiConsole.Ask<int>("Enter the [green blink]mileage (in km)[/]:");
                    int owners = AnsiConsole.Ask<int>("Enter the [green blink]number of previous owners[/]:");
                    int insuranceClaims = AnsiConsole.Ask<int>("Enter the [green blink]number of insurance claims[/]:");
                    Recommendation recommendation = Recommendation.Unknown;
                    List<string> knownIssues = new List<string>();
                    bool addMoreIssues = true;
                    while (addMoreIssues)
                    {
                        string issue = AnsiConsole.Ask<string>("Enter a [green blink]known issue[/] (or type 'done' to finish):");
                        if (issue.ToLower() == "done")
                        {
                            addMoreIssues = false;
                        }
                        else
                        {
                            knownIssues.Add(issue);
                        }
                    }
                    Car newCar = new Car(
                        regNumber: regNumber,
                        brand: brand,
                        model: model,
                        year: year,
                        mileage: mileage,
                        owners: owners,
                        insuranceClaims: insuranceClaims,
                        knownIssues: knownIssues,
                        carAge: new DateTime(year, 1, 1)
                    );
                    // The 'recommendation' property should be set after construction:
                    newCar.Recommendation = Recommendation.Unknown;
                    EvaluateCar(newCar); // تعيين التوصية تلقاتي
                    SaveDataAsJson(newCar);
                    AnsiConsole.MarkupLine("💾 [green]New car added and saved to JSON file![/]");
                    DisplayCarTable(newCar);
                    AddMoreCars = AnsiConsole.Confirm("\nDo you want to add another car?");
                }
            }

        }
        //Show all cars in the datastore JSON file 
        public void ShowAllCarSearchInOneTable()
        {

            Console.Clear();

            // Header
            var header = new FigletText("🚘 All Cars in One Table 🚘")
                .Centered()
                .Color(Color.DarkOliveGreen1_1);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();

            string filePath = "users.json";
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                ShowAllCarsInDataStore();
                return;
            }

            var cars = new List<Car>();
            string existingJson = File.ReadAllText(filePath);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                cars = JsonSerializer.Deserialize<List<Car>>(existingJson) ?? new List<Car>();
            }

            if (cars.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                ShowAllCarsInDataStore();
                return;
            }

            // Decorative rule
            var ruleTitle = new Rule("📋 [bold underline cyan]All Cars in DataStore[/]");
            AnsiConsole.Write(ruleTitle);
            AnsiConsole.WriteLine();

            // Table setup
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .Centered();

            table.AddColumn("[bold]🔢 RegNumber[/]");
            table.AddColumn("[bold]🚗 Brand[/]");
            table.AddColumn("[bold]🛣️ Mileage[/]");
            table.AddColumn("[bold]👥 Owners[/]");
            table.AddColumn("[bold]🧨 KnownIssues[/]");
            table.AddColumn("[bold]🧠 Recommendation[/]");
            table.AddColumn("[bold]📆 CarAge[/]");

            // Add rows
            foreach (var car in cars)
            {
                string recommendationText = car.Recommendation switch
                {
                    Recommendation.RiskyPurchase => "❌ [red]Risky Purchase[/]",
                    Recommendation.Acceptable => "🟡 [yellow]Acceptable[/]",
                    Recommendation.GoodInvestment => "✅ [green]Good Investment[/]",
                    _ => "[grey]Unknown[/]"
                };

                table.AddRow(
                    $"[cyan]{car.RegNumber}[/]",
                    $"[cyan]{car.Brand}[/]",
                    $"[blue]{car.Mileage} km[/]",
                    $"[grey]{car.Owners}[/]",
                    car.KnownIssues.Any() ? string.Join(", ", car.KnownIssues) : "[green]None[/]",
                    recommendationText,
                    $"[cyan]{DateTime.Now.Year - car.Year} years[/]"
                );
            }

            // Display full table
            AnsiConsole.Write(table);
            // write number of cars searched from JSON file to summary line
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                ShowAllCarsInDataStore();
                return;
            }
            var carsCount = cars.Count;
            AnsiConsole.Write(new Markup($"\n[bold]📦 Total cars searched:[/] [green]{carsCount}[/]").Centered());
            //add line disging press Enter to return to menu
            Console.WriteLine();
            AnsiConsole.MarkupLine("\nPress [green]Enter[/] to return show all cars menu...");
            Console.ReadLine();
            ShowAllCarsInDataStore();
        }
        //show all cars in datastore JSON file 
        public void ShowAllCarsSearchInOneByOne()
        {
            Console.Clear();
            var header = new FigletText("🚘 All cars Search One by One 🚘")
                .Centered()
                .Color(Color.DarkOliveGreen1_1);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();
            string filePath = "users.json";
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                ShowAllCarsInDataStore();
                return;
            }
            var cars = new List<Car>();
            string existingJson = File.ReadAllText(filePath);
            if (!string.IsNullOrWhiteSpace(existingJson))
            {
                cars = JsonSerializer.Deserialize<List<Car>>(existingJson) ?? new List<Car>();
            }
            if (cars.Count == 0)
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                ShowAllCarsInDataStore();
                return;
            }
            foreach (var car in cars)
            {
                DisplayCarTable(car);
                //AnsiConsole.MarkupLine("\nPress [green]Enter[/] to view the next car...");
                //Console.ReadLine();
            }
            AnsiConsole.MarkupLine("\n[bold]📦 End of car list.[/]");
            Console.ReadLine();
            ShowAllCarsInDataStore();
        }
        public void ShowAllCarsInDataStore()
        {
            Console.Clear();
            var header = new FigletText("🚘 Show All Cars 🚘")
                .Centered()
                .Color(Color.DarkOliveGreen1_1);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();

            var selectionPrompt = new SelectionPrompt<string>()
                .Title("📂 [bold underline]Select an option[/]:")
                .AddChoices(new[]
                {
                    "📋 Show All Cars in One Table",
                    "📄 Show All Cars One by One",
                    "🔙 Back to Menu"
                });
            var selection = AnsiConsole.Prompt(selectionPrompt);
            if (selection == "🔙 Back to Menu")
            {
                return; // Return to menu
            }
            else if (selection == "📋 Show All Cars in One Table")
            {
                ShowAllCarSearchInOneTable();
            }
            else if (selection == "📄 Show All Cars One by One")
            {
                ShowAllCarsSearchInOneByOne();
            }
        }

        // Remove car from JSON file by registration number by selecting registration number from a list of cars in the JSON file
        //If I don't want to remove the car, go back to menu select Back to menu
        public void RemoveCarByRegNumber()
        {
            bool removeMoreCars = true;
            while (removeMoreCars)
            {
                Console.Clear();
                var header = new FigletText("🚘 Remove Car by RegNumber 🚘")
                    .Centered()
                    .Color(Color.Red3_1);
                AnsiConsole.Write(header);
                AnsiConsole.WriteLine();
                string filePath = $"users.json";
                if (!File.Exists(filePath))
                {
                    AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                    Console.ReadLine();
                    return;
                }
                else
                {
                    var cars = new List<Car>();
                    string existingJson = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(existingJson))
                    {
                        cars = JsonSerializer.Deserialize<List<Car>>(existingJson) ?? new List<Car>();
                    }
                    if (cars.Count == 0)
                    {
                        AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                        Console.ReadLine();
                        return;
                    }
                    else
                    {
                        var carChoices = cars.Select(c => c.RegNumber).ToList();
                        carChoices.Add("🔙 Back to Menu");
                        var menu = new SelectionPrompt<string>()
                            .Title("Select the [red blink]registration number[/] of the car to remove:")
                            .AddChoices(carChoices);
                        var choice = AnsiConsole.Prompt(menu);
                        if (choice == "🔙 Back to Menu")
                        {
                            return; // Go back to menu
                        }
                        var carToRemove = cars.FirstOrDefault(c => c.RegNumber == choice);
                        if (carToRemove != null)
                        {
                            cars.Remove(carToRemove);
                            var options = new JsonSerializerOptions { WriteIndented = true };
                            string updatedJson = JsonSerializer.Serialize(cars, options);
                            File.WriteAllText(filePath, updatedJson);
                            AnsiConsole.MarkupLine($"🗑️ [red]Car with registration number {choice} has been removed.[/]");
                            Console.ReadLine();
                        }
                    }
                }
            }
        }
        //Metod för att rensa alla bilar från JSON-filen
        public void ClearAllCarsFromJson()
        {
            Console.Clear();
            var header = new FigletText("🚘 Clear All Cars Search 🚘")
                .Centered()
                .Color(Color.Red3_1);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();

           //Show all cars regnumber from user.json file if inlogged by confirmation
           //If user has no car in JSON file show massege if not show all car registration numbers and ask for confirmation to delete all cars
            string filePath = $"users.json";
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                return;
            }
            else
            {
                var cars = new List<string>();
                string existingJson = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(existingJson))
                {
                    List<Car> carsSearch = JsonSerializer.Deserialize<List<Car>>(existingJson) ?? new List<Car>();
                    cars = carsSearch.Select(c => c.RegNumber).ToList();

                }
                if (cars.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                    Console.ReadLine();
                    return;
                }
                else
                {
                    AnsiConsole.MarkupLine("The following cars will be [red blink]removed[/]:\n");
                    foreach (var carReg in cars)
                    {
                        AnsiConsole.MarkupLine($"- {carReg}");
                    }
                    bool confirmClear = AnsiConsole.Confirm("\nAre you sure you want to [red blink]clear all cars[/] from the JSON file?");
                    if (confirmClear)
                    {
                        File.WriteAllText(filePath, "[]");
                        AnsiConsole.MarkupLine("🧹 [red]All cars have been removed from the JSON file.[/]");
                        Console.ReadLine();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("❌ [yellow]Operation cancelled. No cars were removed.[/]");
                        Console.ReadLine();
                    }
                }
            }


        }
        //find car by regnumber in JSON fileand return car info by selecting regnumber from a list of cars in the JSON file
        public void FindCarInJsonByRegNumber()
        {
            Console.Clear();
            var header = new FigletText("🚘 Find Car by RegNumber 🚘")
                .Centered()
                .Color(Color.DarkOliveGreen1_1);
            AnsiConsole.Write(header);
            AnsiConsole.WriteLine();
            string filePath = $"users.json";
            if (!File.Exists(filePath))
            {
                AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                Console.ReadLine();
                return;
            }
            else
            {
                var cars = new List<Car>();
                string existingJson = File.ReadAllText(filePath);
                if (!string.IsNullOrWhiteSpace(existingJson))
                {
                    cars = JsonSerializer.Deserialize<List<Car>>(existingJson) ?? new List<Car>();
                }
                if (cars.Count == 0)
                {
                    AnsiConsole.MarkupLine("[grey]No cars found in the JSON file.[/]");
                    Console.ReadLine();
                    return;
                }
                else
                {
                    var carChoices = cars.Select(c => c.RegNumber).ToList();
                    var menu = new SelectionPrompt<string>()
                        .Title("Select the [green blink]registration number[/] of the car to find:")
                        .AddChoices(carChoices);
                    var choice = AnsiConsole.Prompt(menu);
                    var carToFind = cars.FirstOrDefault(c => c.RegNumber == choice);
                    if (carToFind != null)
                    {
                        DisplayCarTable(carToFind);
                        AnsiConsole.MarkupLine("\nPress [green]Enter[/] to return to the menu...");
                        Console.ReadLine();
                    }
                }
            }
        }
        //Main menu for CarSearch class by selecting options from a list 
        public void CarSearchMenu()
        {
            bool continueRunning = true;
            while (continueRunning)
            {
                Console.Clear();
                var header = new FigletText("🚘 Car Search Menu 🚘")
                    .Centered()
                    .Color(Color.DarkOliveGreen1_1);
                AnsiConsole.Write(header);
                AnsiConsole.WriteLine();
                var menu = new SelectionPrompt<string>()
                    .Title("📂 [yellow]Select an option:[/]")
                    .AddChoices(new[]
                    {
                        "🔎 Search Car by RegNumber",
                        "➕ Add New Car",
                        "🔍 Find Car by RegNumber",
                        "📋 Show All Cars",
                        "🗑️ Remove Car by RegNumber",
                        "🧹 Clear All Cars from JSON",
                        "❌ Exit"
                    });
                var choice = AnsiConsole.Prompt(menu);
                switch (choice)
                {
                    case "🔎 Search Car by RegNumber":
                        SearchByRegNumber();
                        break;
                    case "➕ Add New Car":
                        AddNewCarFromUserInput();
                        break;
                    case "🔍 Find Car by RegNumber":
                        FindCarInJsonByRegNumber();
                        break;
                    case "📋 Show All Cars":
                        ShowAllCarsInDataStore();
                        break;
                    case "🗑️ Remove Car by RegNumber":
                        RemoveCarByRegNumber();
                        break;
                    case "🧹 Clear All Cars from JSON":
                        ClearAllCarsFromJson();
                        break;
                    case "❌ Exit":
                        continueRunning = false;
                        break;
                }
            }
        }
    }
}


