
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
//using System.Text.Json;
//using System.IO;
namespace AutoCompare
{
    //Klass
    public class DataStore<T>
    {
        // //privat attribut.
        //public List<User> userStore = new List<User>();//get; set;???
        //public List<Car> carStore = new List<Car>();
        //public List<CarSearch> carSearchStore = new List<CarSearch>();
       
        public List<T> List { get; set; } = new List<T>();


        //Lägga till objekt
        //public void AddItem(User user)
        //{
        //    userStore.Add(user);
        //    SaveToJson("users.json");
        //}

        //Metod för att lägga till objekt av generisk typ T
        public void AddItem(T item)
        {
            List.Add(item);
            SaveToJson($"{typeof(T).Name.ToLower()}s.json");
        }

        public bool RemoveItem(T item)
        {
            bool removed = List.Remove(item);
            if (removed)
                SaveToJson($"{typeof(T).Name.ToLower()}s.json");
            return removed;
        }

        public T? FindItem(Predicate<T> predicate)//LINQ?
        {
            return List.Find(predicate);
        }


        //public List<string>? FindItemHistory(string username)
        //{
        //    User? user = userStore.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        //    return user?.SearchHistory;
        //}


        //public User? FindUser(string username)
        //{
        //    return userStore.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        //}



        //#Steg 2
        public void SaveToJson(string filename)
        {
            try
            {
                // Serialisera listan och skriv till fil
                var json = JsonSerializer.Serialize(List, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, json);
            }
            catch (Exception ex)
            {
                // Logga felet med statisk Logger
                Logger.Log("system", $"DataStore<{typeof(T).Name}>.SaveToJson", ex.ToString());
            }
        }

        public void LoadFromJson(string filename)
        {
            if (File.Exists(filename))
            {
                try
                {
                    var json = File.ReadAllText(filename);
                    List = JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
                }
                catch (Exception ex)
                {
                    Logger.Log("system", $"DataStore<{typeof(T).Name}>.LoadFromJson", ex.ToString());
                    List = new List<T>(); // fallback
                }
            }
            else
            {
                // Filen finns inte → fallback direkt
                List = new List<T>();
            }
        }
    }
}


