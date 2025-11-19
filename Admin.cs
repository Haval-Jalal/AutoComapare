using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCompare
{

//Den här metoden saknas i slutet av koden.
//PrintError() (privat)
    public class Admin
    {
        private readonly DataStore<User> _userStore;
        private readonly Logger _logger;

        // Hardcoded admin credentials (can be moved to JSON later)
        private const string AdminUsername = "admin.autocompare@gmail.com";
        private const string AdminPassword = "Admin123!";

        public bool IsLoggedIn { get; private set; }

        public Admin(DataStore<User> userStore, Logger logger)
        {
            _userStore = userStore;
            _logger = logger;
        }

        // ===============================
        // ADMIN LOGIN (HIDDEN)
        // ===============================
        public bool TryLogin(string username, string password)
        {
            try
            {
                if (username == AdminUsername && password == AdminPassword)
                {
                    IsLoggedIn = true;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("Admin access granted.");
                    Console.ResetColor();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin Login Error]: {ex.Message}");
                return false;
            }
        }

        // ===============================
        // LIST ALL USERS
        // ===============================
        public void ShowAllUsers()
        {
            try
            {
                Console.WriteLine("\n=== ALL USERS ===");

                foreach (var user in _userStore.List)
                    Console.WriteLine($"• {user.Username} (Registered: {user.RegisteredAt})");

                Console.WriteLine("=================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin Error – ShowAllUsers]: {ex.Message}");
            }
        }

        // ===============================
        // DELETE USER
        // ===============================
        public void DeleteUser(string username)
        {
            try
            {
                var user = _userStore.List
                    .FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

                if (user == null)
                {
                    Console.WriteLine("User not found.");
                    return;
                }

                _userStore.RemoveItem(user);
                Console.WriteLine($"User '{username}' has been deleted.");

                _logger.Log("admin", $"Deleted user: {username}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin Error – DeleteUser]: {ex.Message}");
            }
        }

        // ===============================
        // VIEW LOG FILES
        // ===============================
        public void ShowLogFiles()
        {
            try
            {
                Console.WriteLine("\n=== LOG FILES ===");

                if (!Directory.Exists("logs"))
                {
                    Console.WriteLine("No log directory found.");
                    return;
                }

                var files = Directory.GetFiles("logs", "*.log");

                if (!files.Any())
                {
                    Console.WriteLine("No log files found.");
                    return;
                }

                foreach (var file in files)
                    Console.WriteLine("• " + Path.GetFileName(file));

                Console.WriteLine("==================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin Error – ShowLogFiles]: {ex.Message}");
            }
        }

        // ===============================
        // VIEW LOG CONTENT
        // ===============================
        public void ShowLogEntries(string filename)
        {
            try
            {
                string path = Path.Combine("logs", filename);

                if (!File.Exists(path))
                {
                    Console.WriteLine("Log file does not exist.");
                    return;
                }

                Console.WriteLine($"\n=== CONTENT OF {filename} ===");

                foreach (var line in File.ReadAllLines(path))//Lines??
                    Console.WriteLine(line);

                Console.WriteLine("==============================\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin Error – ShowLogEntries]: {ex.Message}");
            }
        }
    }
}