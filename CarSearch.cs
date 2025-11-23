using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCompare
{
    public class CarSearch
    {
        //Metoder för att söka bil baserat på registreringsnummer
        public void SearchByRegNumber(string regNumber)
        {
            try
            {
                bool continueRunning = true;
                while (continueRunning)
                {
                    Console.Clear();

                    // Användarinput via Spectre.Console
                    regNumber = AnsiConsole.Ask<string>("Enter the [green]registration number[/] of the car to evaluate:");

                    // Hämta bildata
                    Car car = GetDummyData(regNumber) ?? throw new NullReferenceException("Car data could not be generated.");

                    // Utvärdera bilen
                    EvaluateCar(car);

                    // Visa bilinfo i tabell
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

                    // Fråga användaren om de vill fortsätta
                    continueRunning = AnsiConsole.Confirm("\nDo you want to evaluate another car?");
                }
            }
            catch (Exception ex)
            {
                // Logga och informera användaren
                Logger.Log("system", "CarSearch.SearchByRegNumber", ex.ToString());
                Console.WriteLine("An error occurred while searching for the car.");
            }
        }


        //Metod för att hämta dummydata för en bil
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
        //Metod för att utvärdera en bil
        public void EvaluateCar(Car car)
        {
            int carAge = DateTime.Now.Year - car.Year;
            string message;
            Spectre.Console.Color panelColor; // Explicitly use Spectre.Console.Color

            if (car.Mileage > 200000 && carAge > 12)
            {
                car.Recommendation = Recommendation.RiskyPurchase;
                message = "[red]Risky Purchase:[/] High mileage, several known issues, many previous owners, and an older vehicle age.";
                panelColor = Spectre.Console.Color.Red;
            }
            else if (car.Mileage < 100000 && carAge < 5)
            {
                car.Recommendation = Recommendation.GoodInvestment;
                message = "[green]Good Investment:[/] Low mileage, minimal issues, few previous owners, and a relatively new model.";
                panelColor = Spectre.Console.Color.Green;
            }
            else
            {
                car.Recommendation = Recommendation.Acceptable;
                message = "[yellow]Acceptable:[/] Moderate mileage, some known issues, average ownership history, or mid-range vehicle age.";
                panelColor = Spectre.Console.Color.Yellow;
            }

            var panel = new Panel(message)
                .Header("Car Evaluation", Justify.Center)
                .Border(BoxBorder.Rounded)
                .BorderStyle(new Style(panelColor))
                .Padding(1, 1, 1, 1);

            AnsiConsole.Write(panel);
        }
    }
}