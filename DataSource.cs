using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using HtmlAgilityPack;
using Spectre.Console;

namespace AutoCompare
{
    public static class DataSource
    {
        //Search car by registration number fake data source for testing 
        public static Car SearchByRegNumber(string regNumber)
        {
            return GetDummyData(regNumber);
        }

        //Fake data generator for testing 
        public static Car GetDummyData(string regNumber)
        {
           Random random = new Random();
           string[] makes = { "Toyota", "Ford", "BMW", "Audi", "Honda", "Tesla", "Chevrolet", "Nissan" };
            string[] models = { "Model A", "Model B", "Model C", "Model D", "Model E" };

           return new Car(
               regNumber: regNumber,
               make: makes[random.Next(makes.Length)],
               model: models[random.Next(models.Length)],
               year: random.Next(2000, 2023),
               mileage: random.Next(0, 200000),
               price: random.Next(50000, 500000)
           );
        }

        // Evaluate car condition based on year and mileage 
        public static string EvaluateCar(Car car)
        {
            if (car.Year >= 2021 && car.Mileage < 30000)
                return "[bold green]Excellent condition[/]";
            else if (car.Year >= 2017 && car.Mileage < 80000)
                return "[bold green]Good condition[/]";
            else if (car.Year >= 2010 && car.Mileage < 150000)
                return "[bold yellow]Fair condition[/]";
            else
                return "[bold red]Needs inspection[/]";
        }
    }
}