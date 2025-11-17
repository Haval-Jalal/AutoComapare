using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Collections.Generic;
using Twilio; 
using Twilio.Rest.Api.V2010.Account;

namespace AutoCompare
{
    // Enum that defines the available 2FA (Two-Factor Authentication) methods
    public enum TwoFactorMethod
    {
        none,
        Email,
        SMS
    }

    // Password validator
    public static class PasswordValidator
    {
        public static bool IsStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 6) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) return false;
            return true;
        }
    }

    public class User
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string Username { get; private set; } = string.Empty;
        public string PasswordHash { get; private set; } = string.Empty;
        public DateTime RegisteredAt { get; private set; }
        public TwoFactorMethod TwoFactorChoice { get; private set; }
        public string? Email { get; private set; }
        public string? PhoneNumber { get; private set; }
        public List<string> SearchHistory { get; } = new();

        public User() { }

        // STARRED password input
        public static string ReadHiddenPassword()
        {
            StringBuilder input = new();
            ConsoleKey key;

            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                key = keyInfo.Key;

                if (key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key == ConsoleKey.Backspace)
                {
                    if (input.Length > 0)
                    {
                        input.Remove(input.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    input.Append(keyInfo.KeyChar);
                    Console.Write("*");
                }
            }

            return input.ToString();
        }

        // REGISTER
        public bool Register(string username, string plainPassword, TwoFactorMethod method, string contact, DataStore<User> userStore)
        {
            userStore.LoadFromJson("users.json");
            if (userStore.List.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine("Username already taken, try again!");
                return false;
            }

            if (!PasswordValidator.IsStrong(plainPassword))
            {
                Console.WriteLine("Password is not strong enough, try again!");
                return false;
            }

            Username = username.Trim();
            PasswordHash = Sha256(plainPassword);
            TwoFactorChoice = method;

            if (method == TwoFactorMethod.Email)
                Email = contact?.Trim();
            else if (method == TwoFactorMethod.SMS)
                PhoneNumber = contact?.Trim();
            else
            {
                Email = null;
                PhoneNumber = null;
            }

            RegisteredAt = DateTime.UtcNow;
            return true;
        }

        // LOGIN
        public bool Login(string enteredPassword)
        {
            if (!CheckPassword(enteredPassword))
            {
                Console.WriteLine("Wrong password.");
                return false;
            }

            // If user has both email and phone number → allow choice including 'none'
            if ((Email != null || PhoneNumber != null))
            {
                Console.WriteLine("Choose verification method:");
                Console.WriteLine("0. None");
                if (Email != null) Console.WriteLine("1. Email");
                if (PhoneNumber != null) Console.WriteLine("2. SMS");
                Console.Write("Your choice: ");

                var choice = Console.ReadLine();
                switch (choice)
                {
                    case "0":
                        TwoFactorChoice = TwoFactorMethod.none;
                        break;
                    case "1":
                        if (Email != null) TwoFactorChoice = TwoFactorMethod.Email;
                        else { Console.WriteLine("Invalid choice."); return false; }
                        break;
                    case "2":
                        if (PhoneNumber != null) TwoFactorChoice = TwoFactorMethod.SMS;
                        else { Console.WriteLine("Invalid choice."); return false; }
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        return false;
                }
            }
            else
            {
                // User has no contact info → 2FA none
                TwoFactorChoice = TwoFactorMethod.none;
            }

            // 2FA logic removed → now handled externally
            return true;
        }

        // FORGOT PASSWORD
        public void ForgotPassword()
        {
            Console.WriteLine("A reset code should be sent via your selected 2FA method.");
        }

        // RESET PASSWORD (AFTER 2FA)
        public bool ResetPassword(string newPassword)
        {
            if (!PasswordValidator.IsStrong(newPassword))
            {
                Console.WriteLine("Password is not strong enough.");
                return false;
            }

            PasswordHash = Sha256(newPassword);
            Console.WriteLine("Password updated successfully.");
            return true;
        }

        public void Logout()
        {
            Console.WriteLine($"{Username} logged out.");
        }

        public void DeleteAccount(DataStore<User> userStore)
        {
            userStore.LoadFromJson("users.json");

            bool removed = userStore.RemoveItem(this);
            if (removed)
                Console.WriteLine($"{Username} account deleted.");
            else 
                Console.WriteLine("Error deleting account.");
        }

        public void GetHistory()
        {
            Console.WriteLine($"Search history for {Username}:");
            foreach (var item in SearchHistory)
                Console.WriteLine(item);
        }

        public bool CheckPassword(string enteredPassword)
        {
            return PasswordHash == Sha256(enteredPassword);
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
    }
}