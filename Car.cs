using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCompare
{
    public class Car
    {
        //Attributes of the car
        public string RegNumber { get; set; }     
        public string Make { get; set; }        
        public string Model { get; set; }      
        public int Year { get; set; }            
        public int Mileage { get; set; }         
        public decimal Price { get; set; }        

        // Constructor to initialize a Car object 
        public Car(string regNumber, string make, string model, int year, int mileage, decimal price)
        {
            RegNumber = regNumber;
            Make = make;
            Model = model;
            Year = year;
            Mileage = mileage;
            Price = price;
        }
    }
}


