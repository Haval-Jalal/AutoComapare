
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
        //Attribut
        public List<User> userStore = new List<User>();
        public List<Car> carStore = new List<Car>();
        

        //Lägga till objekt
        public void AddItem(User user)
        {
            userStore.Add(user);
            SaveToJson("users.json");
        }

        public bool RemoveItem(User user)
        {
            bool removed = userStore.Remove(user); 
            if (removed)
                SaveToJson("users.json");         
            return removed;
        }



        public Car? FindItem(Predicate<Car> predicate)
        {
            return carStore.Find(predicate);
        }

       
        public List<string>? FindItemHistory(string username)
        {
            User? user = userStore.Find(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            return user?.SearchHistory;
        }

        
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


