using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AutoCompare
{
    /// <summary>
    /// User class handles register, login, logout, password reset and search history.
    /// All 2FA sending/verification is delegated to TwoFactorService.
    /// This class uses your existing DataStore<User> (methods: LoadFromJson, SaveToJson, AddItem, RemoveItem, List).
    /// </summary>
    public class User
    {
        // Serializable properties
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Username { get; set; } = string.Empty; // email used as username
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public TwoFactorMethod TwoFactorChoice { get; set; } = TwoFactorMethod.Email;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public List<string> SearchHistory { get; set; } = new();

        // Temporary 2FA storage (in-memory during flows)
        private string? _pending2faCodeHash;
        private DateTime? _pending2faExpiresAt;

        public User() { }

        // -------------------------
        // Hidden password input helper (shows '*' while typing)
        // -------------------------
        public static string ReadHiddenPassword()
        {
            var sb = new StringBuilder();
            while (true)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else
                {
                    sb.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return sb.ToString();
        }

        // -------------------------
        // Hashing & password check
        // -------------------------
        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            return Convert.ToHexString(sha.ComputeHash(bytes));
        }

        public bool CheckPassword(string enteredPassword) => PasswordHash == Sha256(enteredPassword);

        // -------------------------
        // Private password strength validator (keeps project with three main classes)
        // -------------------------
        private static bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrWhiteSpace(password)) return false;
            if (password.Length < 6) return false;
            if (!password.Any(char.IsUpper)) return false;
            if (!password.Any(char.IsLower)) return false;
            if (!password.Any(char.IsDigit)) return false;
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) return false;
            return true;
        }

        // -------------------------
        // Register - interactive flow
        // -------------------------
        public static User? Register(DataStore<User> store, TwoFactorService twoFactor)
        {
            try
            {
                store.LoadFromJson("users.json");

                Console.WriteLine("=== Registration ===");
                Console.Write("Enter your full name (display name): ");
                var displayName = Console.ReadLine()?.Trim() ?? "";

                string email;
                do
                {
                    Console.Write("Enter your email (username): ");
                    email = Console.ReadLine()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        Console.WriteLine("Email must not be empty.");
                        continue;
                    }
                    if (store.List.Any(u => u.Username.Equals(email, StringComparison.OrdinalIgnoreCase)))
                    {
                        Console.WriteLine("Username already taken, try another email.");
                        email = "";
                    }
                } while (string.IsNullOrEmpty(email));

                string password;
                do
                {
                    Console.Write("Enter a strong password: ");
                    password = ReadHiddenPassword();
                    if (!IsPasswordStrong(password))
                        Console.WriteLine("Password not strong enough, please try again.");
                } while (!IsPasswordStrong(password));

                Console.WriteLine("Choose 2FA method: 1) Email  2) SMS");
                var choice = Console.ReadLine();
                var method = choice == "2" ? TwoFactorMethod.SMS : TwoFactorMethod.Email;

                string contact = method == TwoFactorMethod.Email ? email : AskPhone();

                var user = new User
                {
                    Username = email,
                    Email = method == TwoFactorMethod.Email ? contact : null,
                    PhoneNumber = method == TwoFactorMethod.SMS ? contact : null,
                    TwoFactorChoice = method,
                    PasswordHash = Sha256(password),
                    RegisteredAt = DateTime.UtcNow
                };

                // Save user to JSON
                store.AddItem(user);

                // Send 2FA code and verify
                var (codeHash, expiresAt) = twoFactor.SendCode(user, TimeSpan.FromMinutes(5));
                if (string.IsNullOrEmpty(codeHash))
                {
                    Console.WriteLine("Could not send verification code. Please try again later.");
                    return user;
                }

                Console.Write("Enter the code you received: ");
                var entered = Console.ReadLine() ?? "";

                if (twoFactor.VerifyCode(entered, codeHash, expiresAt))
                {
                    Console.WriteLine($"Welcome, {displayName}!");
                    return user;
                }
                else
                {
                    Console.WriteLine("Invalid code. Account saved but verification failed.");
                    return user;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Registration error: {ex.Message}");
                return null;
            }
        }

        // -------------------------
        // Login - interactive flow (offers forgot password on wrong password)
        // -------------------------
        public static User? Login(DataStore<User> store, TwoFactorService twoFactor)
        {
            try
            {
                store.LoadFromJson("users.json");

                Console.WriteLine("=== Login ===");
                Console.Write("Enter your email: ");
                var email = Console.ReadLine()?.Trim() ?? "";

                var user = store.List.FirstOrDefault(u => u.Username.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (user == null)
                {
                    Console.WriteLine("User not found.");
                    return null;
                }

                Console.Write("Enter your password: ");
                var pwd = ReadHiddenPassword();

                if (!user.CheckPassword(pwd))
                {
                    Console.WriteLine("Incorrect password. Would you like to reset your password? (y/n)");
                    var a = Console.ReadLine()?.Trim().ToLower();
                    if (a == "y")
                        user.ForgotPassword(store, twoFactor);
                    return null;
                }

                // Send 2FA code
                var (codeHash, expiresAt) = twoFactor.SendCode(user, TimeSpan.FromMinutes(5));
                if (string.IsNullOrEmpty(codeHash))
                {
                    Console.WriteLine("Could not send 2FA code. Please try again later.");
                    return null;
                }

                Console.Write("Enter the 2FA code: ");
                var entered = Console.ReadLine() ?? "";

                if (twoFactor.VerifyCode(entered, codeHash, expiresAt))
                {
                    Console.WriteLine($"Welcome back, {user.Username}!");
                    return user;
                }
                else
                {
                    Console.WriteLine("Invalid 2FA code.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }

        // -------------------------
        // Forgot password / reset flow
        // -------------------------
        public void ForgotPassword(DataStore<User> store, TwoFactorService twoFactor)
        {
            try
            {
                var (codeHash, expiresAt) = twoFactor.SendCode(this, TimeSpan.FromMinutes(10));
                if (string.IsNullOrEmpty(codeHash))
                {
                    Console.WriteLine("Could not send 2FA code for reset. Try again later.");
                    return;
                }

                Console.Write("Enter the 2FA code you received: ");
                var code = Console.ReadLine() ?? "";

                if (!twoFactor.VerifyCode(code, codeHash, expiresAt))
                {
                    Console.WriteLine("Invalid code. Cannot reset password.");
                    return;
                }

                string newPassword;
                do
                {
                    Console.Write("Enter a new strong password: ");
                    newPassword = ReadHiddenPassword();
                    if (!IsPasswordStrong(newPassword))
                        Console.WriteLine("Password not strong enough, please try again.");
                } while (!IsPasswordStrong(newPassword));

                PasswordHash = Sha256(newPassword);

                // Update JSON store: overwrite item and write file
                var existing = store.List.FirstOrDefault(u => u.Id == this.Id);
                if (existing != null)
                {
                    var idx = store.List.IndexOf(existing);
                    if (idx >= 0)
                    {
                        store.List[idx] = this;
                        store.SaveToJson("users.json");
                    }
                }
                else
                {
                    store.AddItem(this);
                }

                Console.WriteLine("Password successfully reset.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Password reset error: {ex.Message}");
            }
        }

        // -------------------------
        // Delete account
        // -------------------------
        public void DeleteAccount(DataStore<User> store)
        {
            try
            {
                store.LoadFromJson("users.json");
                var removed = store.RemoveItem(this);
                if (removed)
                    Console.WriteLine("Account deleted.");
                else
                    Console.WriteLine("Failed to delete account.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Delete account error: {ex.Message}");
            }
        }

        // -------------------------
        // Helper for phone input
        // -------------------------
        private static string AskPhone()
        {
            Console.Write("Enter phone number (international format, e.g. +467...): ");
            return Console.ReadLine()?.Trim() ?? "";
        }
    }
}