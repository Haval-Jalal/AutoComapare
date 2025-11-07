using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCompare
{
    public class MainMenu
    {
        // Method to run the main program loop
        public void RunProgram()
        {
            bool continueRunning = true;

            while (continueRunning)
            {
                Console.Clear();
                AnsiConsole.MarkupLine("[bold yellow]Welcome to the Car Evaluator![/]");

                string regNumber = AnsiConsole.Ask<string>("Enter [green]car registration number[/]:");

                Car car = DataSource.SearchByRegNumber(regNumber);
                string evaluation = DataSource.EvaluateCar(car);

                AnsiConsole.Write(new Table()
                    .Title("[bold underline]Car Information[/]")
                    .AddColumn("Property")
                    .AddColumn("Value")
                    .AddRow("Registration", car.RegNumber)
                    .AddRow("Make", car.Make)
                    .AddRow("Model", car.Model)
                    .AddRow("Year", car.Year.ToString())
                    .AddRow("Mileage", $"{car.Mileage} km")
                    .AddRow("Price", $"{car.Price} SEK")
                    .AddRow("Evaluation", $"{evaluation}")
                );

                // Logging based on evaluation
                if (car.Mileage > 100000)
                    LogWarning("This car has high mileage.");
                
                Log("Evaluation completed successfully.");

                if (evaluation.Contains("Excellent"))
                    LogInfo("The car is in excellent condition.");
                else if (evaluation.Contains("Good"))
                    LogInfo("The car is in good condition.");
                else if (evaluation.Contains("Fair"))
                    LogWarn("The car is in fair condition.");
                else
                    LogError("The car is in poor condition.");

                continueRunning = AnsiConsole.Confirm("\nDo you want to evaluate another car?");
            }

            AnsiConsole.MarkupLine("\n[bold blue]Thank you for using the Car Evaluator![/]");
            LogInfo("Application ended.");
        }

        // Simple logging methods with different severity levels using Spectre.Console
        public static void Log(string message)
        {
            AnsiConsole.MarkupLine($"[grey]{message}[/]");
        }

        public static void LogError(string message)
        {
            AnsiConsole.MarkupLine($"[red]ERROR: {message}[/]");
        }

        public static void LogWarning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]WARNING: {message}[/]");
        }

        public static void LogInfo(string message)
        {
            AnsiConsole.MarkupLine($"[blue]INFO: {message}[/]");
        }

        public static void LogWarn(string message)
        {
            AnsiConsole.MarkupLine($"[orange3]WARN: {message}[/]");
        }

    }
}
