using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AutoCompare
{
    public class CarSearch
    {
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
            int carAge = DateTime.Now.Year - car.Year;
            string message;
            Spectre.Console.Color panelColor;

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

        // Search loop kept in UIManager (no Save here)
        public void SearchByRegNumberInteractive(string regNumber)
        {
            var car = GetDummyData(regNumber);
            EvaluateCar(car);

            var table = new Table().Title("[bold underline cyan]Car Information[/]");
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
        }
    }
}