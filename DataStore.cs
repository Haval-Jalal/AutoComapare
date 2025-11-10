
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;
//using System.Text.Json;
//using System.IO;
namespace AutoCompare
{
    //Klass
    public class DataStore<T>
    {
         //privat attribut.
        public List<User> userStore = new List<User>();//get; set;???
        public List<Car> carStore = new List<Car>();
        public List<CarSearch> carSearchStore = new List<CarSearch>();
        //user lista
        //car lista istället

        //Lägga till objekt
        public void AddItem(User user)
        {
            userStore.Add(user);
            SaveToJson("users.json");
        }

        public bool RemoveItem(User user)
        {
            bool removed = userStore.Remove(user); // tar bort från listan i minnet
            if (removed)
                SaveToJson("users.json");         // uppdatera JSON-filen
            return removed;
        }



        //FindItem() är kopplad med inloggning! - chatt
        // Hitta en bil i carStore baserat på ett villkor
        //Chatt:
        //Används för att hitta en bil i listan carStore
        public Car? FindItem(Predicate<Car> predicate)
        {
            return carStore.Find(predicate);
        }

        // Hämta sökhistorik från en användare, baserat på användarnamn.
        public List<string>? FindItemHistory(string username)
        {
            User? user = userStore.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return user?.SearchHistory;
        }

        // Hitta en användare baserat på användarnamn
        //jag tänker att FindItem är kopplad med inloggningen dock nu när jag tänker på det
        //T.ex vid inloggning används FindItem () i User listan för att hitta rätt
        //användare baserat på användarnamn.
        //I ditt system kan FindItem på userStore användas exakt så:

        //När någon försöker logga in skickar UI-lagret användarnamnet till DataStore<User>.

        //FindItem söker i listan userStore efter en User med matchande Username.

        //Om en match hittas → returnerar User-objektet → du kan sedan kontrollera lösenord och tvåfaktorsautentisering.

        //Om ingen match hittas → returnerar null → UI-lagret visar felmeddelande.
        public User? FindUser(string username)
        {
            return userStore.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }



        //#Steg 2
        public void SaveToJson(string filename)
        {

        }

        public void LoadFromJson(string filename)
        {

        }
    }
}


