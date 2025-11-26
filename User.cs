using Spectre.Console;
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
            try
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

            catch (Exception ex)
            {
                Logger.Log("User.ReadHiddenPassword:", ex);
                Console.WriteLine("An error occurred while reading password input.");
                return string.Empty;
            }
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
            try
            {
                bool removed = userStore.RemoveItem(this); // RemoveItem will handle saving
                return removed;
            }
            catch (Exception ex)
            {
                Logger.Log("User.DeleteAccount:", ex);
                Console.WriteLine("An error occurred while deleting the account.");
                return false;
            }
        }

        // FORGOT PASSWORD
        public void ForgotPassword(DataStore<User> userStore)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Forgot Password").Centered().Color(Color.Yellow));
            AnsiConsole.Write(new Rule("[yellow]Password Reset[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();

            string username = AnsiConsole.Ask<string>("Enter your [green]email[/]:").Trim();
            var user = userStore.List.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                AnsiConsole.MarkupLine($"\n[red]✗ No account found:[/] [bold]{username}[/]");
                var action = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Would you like to register?")
                    .AddChoices(new[] { "Try again", "Register", "Cancel" }));

                if (action == "Register") Register(string.Empty, string.Empty, TwoFactorMethod.none, null);
                else if (action == "Try again") ForgotPassword(userStore);
                return;
            }

            AnsiConsole.MarkupLine($"[green]✓ Account found:[/] [bold]{username}[/]");
            AnsiConsole.MarkupLine("[yellow]Verify your identity with 2FA.[/]\n");

            while (true)
            {
                bool hasEmail = !string.IsNullOrWhiteSpace(user.Email), hasPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber);
                var choices = new List<string>();
                if (hasEmail) choices.Add("📧 Email");
                if (hasPhone) choices.Add("📱 SMS");
                if (!hasEmail) choices.Add("📧 Add Email");
                if (!hasPhone) choices.Add("📱 Add Phone");
                choices.Add("❌ Cancel");

                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Choose [yellow]2FA method[/]:").AddChoices(choices));
                if (choice == "❌ Cancel") return;

                if (choice.Contains("Add"))
                {
                    bool isEmail = choice.Contains("Email");
                    string val = AnsiConsole.Ask<string>($"Enter [blue]{(isEmail ? "email" : "phone")}[/]:").Trim();
                    if (isEmail) user.Email = val; else user.PhoneNumber = val;
                    userStore.SaveToJson();
                    AnsiConsole.MarkupLine("[green]✓ Saved![/]");
                    continue;
                }

                bool isEmailMethod = choice == "📧 Email";
                string contact = isEmailMethod ? user.Email! : user.PhoneNumber!;
                string label = isEmailMethod ? "email" : "phone";

                AnsiConsole.MarkupLine($"Current {label}: [blue]{contact}[/]");
                if (AnsiConsole.Prompt(new SelectionPrompt<string>().Title("Is this correct?").AddChoices(new[] { "Yes", "No" })) == "No")
                {
                    contact = AnsiConsole.Ask<string>($"Enter new [blue]{label}[/]:").Trim();
                    if (isEmailMethod) user.Email = contact; else user.PhoneNumber = contact;
                    userStore.SaveToJson();
                    AnsiConsole.MarkupLine("[green]✓ Saved![/]");
                }

                AnsiConsole.MarkupLine($"Sending code to: [blue]{contact}[/]");
                if (TwoFactor.Verify(isEmailMethod ? TwoFactorMethod.Email : TwoFactorMethod.SMS, user.Email, user.PhoneNumber))
                {
                    AnsiConsole.MarkupLine("\n[green]✓ Verified![/]\n");
                    while (true)
                    {
                        var newPass = AnsiConsole.Prompt(new TextPrompt<string>("Enter [green]new password[/]:").Secret());
                        var confirm = AnsiConsole.Prompt(new TextPrompt<string>("Confirm:").Secret());
                        if (newPass != confirm) { AnsiConsole.MarkupLine("[red]✗ Mismatch![/]\n"); continue; }
                        if (user.ResetPassword(newPass)) { userStore.SaveToJson(); AnsiConsole.MarkupLine("[green]✓ Password reset![/]"); Console.ReadKey(true); return; }
                        AnsiConsole.MarkupLine("[red]✗ Failed. Retry.[/]");
                    }
                }

                AnsiConsole.MarkupLine("[red]✗ Verification failed.[/]\n");
                if (!AnsiConsole.Confirm($"Try {(isEmailMethod ? "SMS" : "Email")} instead?")) return;
            }
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
            try
            {


                using var sha = SHA256.Create();
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);
                return Convert.ToHexString(hash).ToLowerInvariant();
            }
            catch (Exception ex)
            {
                Logger.Log("User.Sha256:", ex);
                Console.WriteLine("An error occurred while hashing data.");
                return string.Empty;
            }
        }

        public bool AttemptLogin(string enteredPassword)
        {
            if (!CheckPassword(enteredPassword))
            {
                Console.WriteLine("Wrong password!");
                Logger.Log("LoginFailed", new Exception($"User {Username} entered wrong password."));
                return false;
            }
           else
            {
               Console.WriteLine("Login successful!");
                return true;
            }
        }   
    }
}