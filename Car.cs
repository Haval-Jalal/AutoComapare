using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoCompare
{
    public enum Recommendation
    {
        GoodInvestment,
        Acceptable,
        RiskyPurchase
    }

    public class Car
    {
        public string RegNumber { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }
        public int Owners { get; set; }
        public int InsuranceClaims { get; set; }
        public List<string> KnownIssues { get; set; }
        public Recommendation Recommendation { get; set; }
        public DateTime CarAge { get; set; }

        public Car() { } // NEW: parameterless constructor for deserialization

        public Car(string regNumber, string brand, string model, int year, int mileage,
                   int owners, int insuranceClaims, List<string> knownIssues, DateTime carAge)
        {
            try
            {


                RegNumber = regNumber;
                Brand = brand;
                Model = model;
                Year = year;
                Mileage = mileage;
                Owners = owners;
                InsuranceClaims = insuranceClaims;
                KnownIssues = knownIssues ?? new List<string>();
                CarAge = carAge;
                Recommendation = Recommendation.RiskyPurchase;
            }
            catch (Exception ex)
            {
                Logger.Log($"Car Constructor error for {regNumber}:", ex);

                RegNumber = regNumber ?? "UNKNOWN";
                Brand = brand ?? "UNKNOWN";
                Model = model ?? "UNKNOWN";
                Year = year;
                Mileage = mileage;
                Owners = owners;
                InsuranceClaims = insuranceClaims;
                KnownIssues = new List<string>();
                CarAge = carAge;
                Recommendation = Recommendation.RiskyPurchase;
            }
        }

        public void Evaluate()
        {
            if (Mileage > 200000 && KnownIssues.Count > 2 && Owners > 4 && (DateTime.Now.Year - Year) > 10)
                Recommendation = Recommendation.RiskyPurchase;
            else if (Mileage < 100000 && KnownIssues.Count == 0 && InsuranceClaims == 0)
                Recommendation = Recommendation.GoodInvestment;
            else
                Recommendation = Recommendation.RiskyPurchase;
        }

        public void DisplayInfo()
        {
            Console.WriteLine("=== Car info ===");
            Console.WriteLine($"Registration: {RegNumber}");
            Console.WriteLine($"Brand: {Brand}");
            Console.WriteLine($"Model: {Model}");
            Console.WriteLine($"Year: {Year}");
            Console.WriteLine($"Mileage: {Mileage}");
            Console.WriteLine($"Owners: {Owners}");
            Console.WriteLine($"Insurance claims: {InsuranceClaims}");
            Console.WriteLine($"Known issues: {(KnownIssues.Count > 0 ? string.Join(", ", KnownIssues) : "None")}");
            string readableRecommendation = Regex.Replace(Recommendation.ToString(), "(\\B[A-Z])", " $1");
            Console.WriteLine($"Recommendation: {readableRecommendation}");
            Console.WriteLine("================");
        }
    }
}