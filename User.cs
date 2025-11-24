using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoCompare
{
    public static class PasswordValidator
    {
        public static bool IsStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 6) return false;
            if (!System.Linq.Enumerable.Any(password, char.IsUpper)) return false;
            if (!System.Linq.Enumerable.Any(password, char.IsLower)) return false;
            if (!System.Linq.Enumerable.Any(password, char.IsDigit)) return false;
            if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) return false;
            return true;
        }
    }
    // NOTE: TwoFactorMethod enum is defined elsewhere; keep it consistent.
    public class User
    {
        // CHANGED: made setters public so System.Text.Json can set properties during deserialization
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public TwoFactorMethod TwoFactorChoice { get; set; } = TwoFactorMethod.none;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public List<string> SearchHistory { get; set; } = new List<string>();

        public User() { }

        // STARRED: keep the ReadHiddenPassword static helper if you use it
        public static string ReadHiddenPassword()
        {
            StringBuilder input = new StringBuilder();
            while (true)
            {
                var keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (keyInfo.Key == ConsoleKey.Backspace)
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

        // Helps validate email format, so user uses a real email address.
        public static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }


        // CHANGED: Register no longer calls LoadFromJson. It simply sets properties.
        // The caller (UIManager) should call userStore.AddItem(...) which will Save.
        public bool Register(string username, string plainPassword, TwoFactorMethod method, string contact)
        {
            if (!PasswordValidator.IsStrong(plainPassword))
            {
                Console.WriteLine("Password is not strong enough, try again!");
                return false;
            }

            if (!IsValidEmail(username))
            {
                Console.WriteLine("Invalid email format!");
                return false;
            }

            if (method == TwoFactorMethod.Email && (contact == null || !IsValidEmail(contact)))
            {
                Console.WriteLine("Invalid 2FA email format!");
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

        // Check password (unchanged)
        public bool CheckPassword(string enteredPassword)
        {
            return PasswordHash == Sha256(enteredPassword);
        }

        // CHANGED: DeleteAccount does not load json or create new datastore. Caller must pass store.
        public bool DeleteAccount(DataStore<User> userStore)
        {
            bool removed = userStore.RemoveItem(this); // RemoveItem will handle saving
            return removed;
        }

        // FORGOT PASSWORD
        public void ForgotPassword()
        {
            Console.WriteLine("A reset code should be sent via your selected 2FA method.");
        }

        public bool ResetPassword(string newPassword)
        {
            if (!PasswordValidator.IsStrong(newPassword))
            {
                Console.WriteLine("Password is not strong enough.");
                return false;
            }
            PasswordHash = Sha256(newPassword);
            return true;
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}