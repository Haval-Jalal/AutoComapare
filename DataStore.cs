using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCompare
{
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
            //privat attribut
            public List<T> User = new List<T>();
            public List<T> Car = new List<T>();
            //user lista
            //car lista istället

            //Lägga till objekt
            public void AddItem(T item)
            {
                User.Add(item);
                Car.Add(item);
            }

            //Ta bort objekt
            public void RemoveItem(T item)
            {
                User.Remove(item);
                Car.Remove(item);
            }

            //Alternativ #1 - bara user
            //if (!Items.Remove(username))
            //    Console.WriteLine($"The username {username.Name} was not found.");
            //else
            //    Console.WriteLine($"The username {username.Name} was removed.");


            //Alternativ #2 - User och Car
            //public void Remove(T item)
            //{
            //    if (!Items.Remove(item))
            //    {
            //        if (item is User u)
            //            Console.WriteLine($"The username {u.Name} was not found.");
            //        else if (item is Car c)
            //            Console.WriteLine($"The car {c.Brand} {c.Model} was not found.");
            //    }
            //    else
            //    {
            //        if (item is User u)
            //            Console.WriteLine($"The username {u.Name} was removed.");
            //        else if (item is Car c)
            //            Console.WriteLine($"The car {c.Brand} {c.Model} was removed.");
            //    }
            //}

            //Alternativ #3 - User och Car -> ternary operator. Nytt inte lärt oss
            //public void RemoveItem(T item)
            //{
            //    if (item is User u)
            //        Console.WriteLine(!Items.Remove(item)
            //            ? $"The username {u.Name} was not found."
            //            : $"The username {u.Name} was removed.");
            //    else if (item is Car c)
            //        Console.WriteLine(!Items.Remove(item)
            //            ? $"The car {c.Brand} {c.Model} was not found."
            //            : $"The car {c.Brand} {c.Model} was removed.");
            //}

            //Alternativ #4 - item
            //public void RemoveItem(T item)
            //{
            //    if (!Items.Remove(item))
            //        Console.WriteLine("The item was not found.");
            //    else
            //        Console.WriteLine("The item was removed.");
            //}


            //Hitta objekt
            public T FindItem(Predicate<T> predicate)
            {
                //returnerar ett resultat - ett objekt
                return Items.Find(predicate);
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

}
