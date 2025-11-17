using AutoCompare.AutoCompare.AutoCompareApp;

namespace AutoCompare
{
    internal class Program
    {
        static void Main(string[] args)
        {

            //Ställ in konsolens utdata för att stödja UTF-8-tecken
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var userStore = new DataStore<User>();
            userStore.LoadFromJson("users.json");

            var carStore = new DataStore<Car>();
            carStore.LoadFromJson("cars.json");

            var carSearchStore = new DataStore<CarSearch>();
            carSearchStore.LoadFromJson("carSearches.json");

            var ui = new UIManager();
            ui.Start();

        }
    }
}
