
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

        public T? FindItem(Predicate<T> predicate)
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
            var json = System.Text.Json.JsonSerializer.Serialize(List,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filename, json);
        }

        public void LoadFromJson(string filename)
        {
            if (File.Exists(filename))
            {
                var json = File.ReadAllText(filename);
                List = System.Text.Json.JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();


            }
        }
    }
}


