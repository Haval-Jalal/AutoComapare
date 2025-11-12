using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoCompare
{

        //Lägger till en enum för de olika stegen i rekommendationslistan: Bra köp, OK köp, dåligt köp
    public enum Recommendation
    {       
        GoodInvestment,       
        Acceptable,   
        RiskyPurchase
    }


    public class Car
    {
        // Attribut
        public string RegNumber { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public int Year { get; set; }
        public int Mileage { get; set; }
        public int Owners { get; set; }
        public int InsuranceClaims { get; set; }
        public List<string> KnownIssues { get; set; }   //Listan är för närvarande bestämd till <string>. Vi kan återkomma till denna rad om vi stöter på probmel med json
        public Recommendation Recommendation { get; set; }
        public DateTime CarAge { get; set; }


        // Konstruktor
        public Car(string regNumber, string brand, string model, int year, int mileage,
                   int owners, int insuranceClaims, List<string> knownIssues, DateTime carAge) 
        {
            RegNumber = regNumber;
            Brand = brand;
            Model = model;
            Year = year;
            Mileage = mileage;
            Owners = owners;
            InsuranceClaims = insuranceClaims;
            KnownIssues = knownIssues ?? new List<string>();
            CarAge = DateTime.Now;
            Recommendation = Recommendation.RiskyPurchase; 
            CarAge = carAge;
        }


        // Metod för utvärdering av bil
        public void Evaluate()
        {
            if (Mileage > 200000 && KnownIssues.Count > 2 && Owners > 4 && (DateTime.Now.Year - Year) > 10)
            {
                Recommendation = Recommendation.RiskyPurchase;

            }
            else if (Mileage < 100000 && KnownIssues.Count == 0 && InsuranceClaims == 0)
            {
                Recommendation = Recommendation.GoodInvestment;
            }
            else
            {
                Recommendation = Recommendation.RiskyPurchase;
            }
        }

        //Metod för information om bil
        public void DisplayInfo()
        {
            Console.WriteLine("=== Bilinformation ===");
            Console.WriteLine($"Registreringsnummer: {RegNumber}");
            Console.WriteLine($"Märke: {Brand}");
            Console.WriteLine($"Modell: {Model}");
            Console.WriteLine($"Årsmodell: {Year}");
            Console.WriteLine($"Miltal: {Mileage}");
            Console.WriteLine($"Antal ägare: {Owners}");
            Console.WriteLine($"Försäkringsärenden: {InsuranceClaims}");
            Console.WriteLine($"Kända problem: {(KnownIssues.Count > 0 ? string.Join(", ", KnownIssues) : "Inga")}");
            string readableRecommendation = Regex.Replace(Recommendation.ToString(), "(\\B[A-Z])", " $1");  //Skriver om rekommendationer för att få mellanrum när de skrivs ut
            Console.WriteLine($"Rekommendation: {readableRecommendation}");
            Console.WriteLine("======================");
        }
    }


}

