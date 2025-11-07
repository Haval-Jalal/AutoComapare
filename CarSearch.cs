using HtmlAgilityPack;
using Spectre.Console;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AutoCompare
{
    public class CarSearch
    {
        //Hämta bilinformation baserat på registreringsnummer 
        public Car SearchByRegNumber(string regNumber)
        {
            return GetDummyData(regNumber);
        }
        //Generera dummydata för bil baserat på registreringsnummer
        public static Car GetDummyData(string regNumber)
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
                mileage: random.Next(0, 200000),
                owners: random.Next(1, 7),
                insuranceClaims: random.Next(0, 5),
                knownIssues: issues,
                carAge: carAge
            );
        }
        //- EvaluateCar(Car car)
        public static string EvaluateCar(Car car)
        {
            if (car.Mileage > 200000 && car.KnownIssues.Count > 2 && car.Owners > 4 && (DateTime.Now.Year - car.Year) > 10)
            {
                car.Recommendation = Recommendation.RiskyPurchase;
                return "[red]Risky Purchase: High mileage, multiple known issues, many previous owners, and older age.[/]";
            }
            else if (car.Mileage > 150000 || car.KnownIssues.Count > 1 || car.Owners > 2 || (DateTime.Now.Year - car.Year) > 5)
            {
                car.Recommendation = Recommendation.Acceptable;
                return "[yellow]Acceptable: Moderate mileage, some known issues, a few previous owners, or middle age.[/]";
            }
            else
            {
                car.Recommendation = Recommendation.GoodInvestment;
                return "[green]Good Investment: Low mileage, no known issues, few previous owners, and newer age.[/]";
            }
        }

        //save data as JSON in car.json file 
        public static void SaveDataAsJson(Car car, string filePath)
        {
            try
            {
                string jsonString = JsonSerializer.Serialize(car);
                // Lägg till ny rad för varje bil, utan att ta bort tidigare data
                File.AppendAllText(filePath, jsonString + Environment.NewLine);
                AnsiConsole.MarkupLine($"[blue]Car data saved successfully to {filePath}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]ERROR: Failed to save car data: {ex.Message}[/]");
            }
        }

        //Metod for run car search by select menu
        public static void SearchCar()
        {
            bool continueRunning = true;
            while (continueRunning)
            {
                Console.Clear();
                AnsiConsole.MarkupLine("[bold yellow]Welcome to the Car Evaluator![/]");
                string regNumber = AnsiConsole.Ask<string>("Enter [green]car registration number[/]:");
                // Search for car by registration number 
                CarSearch carSearch = new CarSearch();
                Car car = carSearch.SearchByRegNumber(regNumber);
                string evaluation = CarSearch.EvaluateCar(car);

                // Display car information in a table format from Spectre.Console 
                var table = new Table()
                    .Title("[bold underline cyan]Car Information[/]");
                table.AddColumn("Property");
                table.AddColumn("Value");

                table.AddRow("Registration", car.RegNumber);
                table.AddRow("Make", car.Brand);
                table.AddRow("Model", car.Model);
                table.AddRow("Year", car.Year.ToString());
                table.AddRow("Mileage", $"{car.Mileage} km");
                table.AddRow("Owners", car.Owners.ToString());
                table.AddRow("Insurance Claims", car.InsuranceClaims.ToString());
                table.AddRow("Known Issues", string.Join(", ", car.KnownIssues));
                //Display recommendation with color coding 
                string recommendationText = car.Recommendation switch
                {
                    Recommendation.RiskyPurchase => "[red]Risky Purchase[/]",
                    Recommendation.Acceptable => "[yellow]Acceptable[/]",
                    Recommendation.GoodInvestment => "[green]Good Investment[/]",
                    _ => "[grey]Unknown[/]"
                };

                table.AddRow("Recommendation", recommendationText);
                table.AddRow("Car Age", $"{DateTime.Now.Year - car.Year} years");

                AnsiConsole.Write(table);
                AnsiConsole.MarkupLine($"[bold underline]Evaluation:[/] {evaluation}");
                //save data as JSON in car.json file
                string filePath = "carInfo.json";
                SaveDataAsJson(car, filePath);
                // Ask user if they want to evaluate another car
                continueRunning = AnsiConsole.Confirm("\nDo you want to evaluate another car?");
            }

           
        }
        //Show all cars that have been saved in carInfo.json file in table format
        public static void ShowSavedCars(string filePath)
        {
            try 
            {
                //Check if file exists and is not empty 
                if (!File.Exists("carInfo.json"))
                {
                    AnsiConsole.MarkupLine("[yellow]WARNING: No saved car data found.[/]");
                    return;
                }
                var lines = File.ReadAllLines("carInfo.json");
                if (lines.Length == 0)
                {
                    AnsiConsole.MarkupLine("[yellow]WARNING: No saved car data found.[/]");
                    return;
                }
                //Read each line from file and deserialize to Car object
                foreach (var line in lines)
                {
                    Car car = JsonSerializer.Deserialize<Car>(line);
                    var table = new Table()
                        .Title("[bold underline cyan]Saved Car Information[/]");
                    table.AddColumn("Property");
                    table.AddColumn("Value");
                    table.AddRow("Registration", car.RegNumber);
                    table.AddRow("Make", car.Brand);
                    table.AddRow("Model", car.Model);
                    table.AddRow("Year", car.Year.ToString());
                    table.AddRow("Mileage", $"{car.Mileage} km");
                    table.AddRow("Owners", car.Owners.ToString());
                    table.AddRow("Insurance Claims", car.InsuranceClaims.ToString());
                    table.AddRow("Known Issues", string.Join(", ", car.KnownIssues));
                    string recommendationText = car.Recommendation switch
                    {
                        Recommendation.RiskyPurchase => "[red]Risky Purchase[/]",
                        Recommendation.Acceptable => "[yellow]Acceptable[/]",
                        Recommendation.GoodInvestment => "[green]Good Investment[/]",
                        _ => "[grey]Unknown[/]"
                    };

                    table.AddRow("Recommendation", recommendationText);
                    table.AddRow("Car Age", $"{DateTime.Now.Year - car.Year} years");
                    AnsiConsole.Write(table);
                    AnsiConsole.MarkupLine($"[bold underline]Evaluation:[/] {CarSearch.EvaluateCar(car)}");

                }
                AnsiConsole.MarkupLine("[blue]INFO: Displayed all saved cars successfully.[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]ERROR: Failed to read saved car data: {ex.Message}[/]");
            }
            //Return to menu prompt
            AnsiConsole.MarkupLine("\nPress any key to return to the menu...");
            Console.ReadKey();
        }


        //metod for select menu search car or show saved cars
        public static void ShowCarSearchMenu()
        {
            bool continueRunning = true;
            while (continueRunning)
            {
                Console.Clear();
                AnsiConsole.MarkupLine("[bold yellow]Car Evaluation Menu[/]");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select an option:")
                        .PageSize(10)
                        .AddChoices(new[] {
                    "Search for a car",
                    "Show saved cars",
                    "Exit"
                        }));

                switch (choice)
                {
                    case "Search for a car":
                        SearchCar();
                        break;
                    case "Show saved cars":
                        ShowSavedCars("carInfo.json");
                        break;
                    case "Exit":
                        continueRunning = false;
                        break;
                    default:
                        AnsiConsole.MarkupLine("[red]ERROR: Invalid selection. Please try again.[/]");
                        break;
                }
            }
        }

    }
}