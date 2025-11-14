using System;
using DotNetEnv;
using System;

namespace AutoCompare
{
    // Program.cs only starts the UI manager.
    internal class Program
    {
        static void Main(string[] args)
        {
            Env.Load();
            Console.Clear();
            try
            {
                var ui = new UIManager();
                ui.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}