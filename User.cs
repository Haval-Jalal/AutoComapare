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

        // CHANGED: Register no longer calls LoadFromJson. It simply sets properties.
        // The caller (UIManager) should call userStore.AddItem(...) which will Save.
        public bool Register(string username, string plainPassword, TwoFactorMethod method, string contact)
        {
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
        public void ForgotPassword(DataStore<User> userStore)
        {
            Console.Clear();
            AnsiConsole.Write(new FigletText("Forgot Password").Centered().Color(Color.Yellow));
            AnsiConsole.Write(new Rule("[yellow]Password Reset[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();

            // Find user
            string username = AnsiConsole.Ask<string>("Enter your [green]username[/]:").Trim();
            var user = userStore.List.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                AnsiConsole.MarkupLine($"\n[red]✗ No account found:[/] [bold]{username}[/]");
                if (AnsiConsole.Confirm("Would you like to register?"))
                    Register(string.Empty, string.Empty, TwoFactorMethod.none, null); // Call to UIManager.Register would be better
                return;
            }

            AnsiConsole.MarkupLine($"[green]✓ Account found:[/] [bold]{username}[/]");
            AnsiConsole.MarkupLine("[yellow]Please verify your identity with 2FA.[/]\n");

            bool hasEmail = !string.IsNullOrWhiteSpace(user.Email);
            bool hasPhone = !string.IsNullOrWhiteSpace(user.PhoneNumber);

            // 2FA verification loop
            while (true)
            {
                var choices = new List<string>();
                if (hasEmail) choices.Add("📧 Email");
                if (hasPhone) choices.Add("📱 SMS");
                if (!hasEmail) choices.Add("📧 Add Email");
                if (!hasPhone) choices.Add("📱 Add Phone");
                choices.Add("❌ Cancel");

                var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Choose [yellow]2FA method[/]:")
                    .AddChoices(choices));

                if (choice == "❌ Cancel") { AnsiConsole.MarkupLine("[grey]Cancelled.[/]"); return; }

                // Add missing contact info
                if (choice == "📧 Add Email")
                {
                    user.Email = AnsiConsole.Ask<string>("Enter [blue]email[/]:").Trim();
                    userStore.SaveToJson();
                    hasEmail = true;
                    AnsiConsole.MarkupLine("[green]✓ Saved![/]");
                    continue;
                }
                if (choice == "📱 Add Phone")
                {
                    user.PhoneNumber = AnsiConsole.Ask<string>("Enter [blue]phone[/]:").Trim();
                    userStore.SaveToJson();
                    hasPhone = true;
                    AnsiConsole.MarkupLine("[green]✓ Saved![/]");
                    continue;
                }

                bool isEmail = choice == "📧 Email";
                string contact = isEmail ? user.Email! : user.PhoneNumber!;
                string label = isEmail ? "email" : "phone";

                // Confirm or update contact
                AnsiConsole.MarkupLine($"Current {label}: [blue]{contact}[/]");
                if (!AnsiConsole.Confirm("Is this correct?"))
                {
                    contact = AnsiConsole.Ask<string>($"Enter new [blue]{label}[/]:").Trim();
                    if (isEmail) user.Email = contact; else user.PhoneNumber = contact;
                    userStore.SaveToJson();
                    AnsiConsole.MarkupLine("[green]✓ Saved![/]");
                }

                // Send and verify code
                AnsiConsole.MarkupLine($"Sending code to: [blue]{contact}[/]");
                var method = isEmail ? TwoFactorMethod.Email : TwoFactorMethod.SMS;

                if (TwoFactor.Verify(method, user.Email, user.PhoneNumber))
                {
                    AnsiConsole.MarkupLine("\n[green]✓ 2FA verified![/]\n");

                    // Password reset with confirmation
                    while (true)
                    {
                        var newPass = AnsiConsole.Prompt(new TextPrompt<string>("Enter [green]new password[/]:").Secret());
                        var confirm = AnsiConsole.Prompt(new TextPrompt<string>("Confirm [green]password[/]:").Secret());

                        if (newPass != confirm)
                        {
                            AnsiConsole.MarkupLine("[red]✗ Passwords do not match![/]\n");
                            continue;
                        }

                        if (user.ResetPassword(newPass))
                        {
                            userStore.SaveToJson();
                            AnsiConsole.MarkupLine("[green]✓ Password reset successfully![/]");
                            Console.ReadKey(true);
                            return;
                        }
                        AnsiConsole.MarkupLine("[red]✗ Reset failed. Try again.[/]");
                    }
                }

                // Verification failed
                AnsiConsole.MarkupLine("[red]✗ Verification failed.[/]\n");
                bool canSwitch = isEmail ? hasPhone : hasEmail;
                string other = isEmail ? "SMS" : "Email";

                if (canSwitch && !AnsiConsole.Confirm($"Try {other} instead?"))
                    return;
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
            using var sha = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = sha.ComputeHash(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }
    }
}