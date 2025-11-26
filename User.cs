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
        public TwoFactorMethod TwoFactorChoice { get; set; } = TwoFactorMethod.Email;
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
        public void ForgotPassword(DataStore<User> userStore)
        {
            // Display header and layout for the screen
            Console.Clear();
            AnsiConsole.Write(new FigletText("Forgot Password").Centered().Color(Color.Yellow));
            AnsiConsole.Write(new Rule("[yellow]Password Reset[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();

            // Ask for the username (email) and try to find the user in the data store
            string username = AnsiConsole.Ask<string>("Enter your [green]email[/]:").Trim();
            var user = userStore.List.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            // If no matching account is found, show options and exit or redirect
            if (user == null)
            {
                AnsiConsole.MarkupLine($"\n[red]✗ No account found:[/] [bold]{username}[/]");
                var action = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Would you like to register?")
                    .AddChoices(new[] { "Try again", "Register", "Cancel" }));

                if (action == "Register")
                {
                    // Cannot call UIManager.Register() from here — instruct user to use the Register menu
                    AnsiConsole.MarkupLine("[yellow]Please use the Register option from the main menu.[/]");
                    Console.ReadKey(true);
                    return;
                }
                else if (action == "Try again")
                {
                    ForgotPassword(userStore);
                    return;
                }
                else // Cancel
                {
                    return;
                }
            }

            // Confirm that the account exists
            AnsiConsole.MarkupLine($"[green]✓ Account found:[/] [bold]{username}[/]");
            AnsiConsole.MarkupLine("[yellow]Verify your identity with your registered 2FA method.[/]\n");

            // Ensure the user has a saved 2FA method (Email or SMS)
            if (user.TwoFactorChoice != TwoFactorMethod.Email && user.TwoFactorChoice != TwoFactorMethod.SMS)
            {
                AnsiConsole.MarkupLine("[red]✗ No 2FA method registered on this account. Contact admin for help.[/]");
                Console.ReadKey(true);
                return;
            }

            // Determine which contact to use based on the saved 2FA method
            bool isEmailMethod = user.TwoFactorChoice == TwoFactorMethod.Email;
            string contact = isEmailMethod ? user.Email ?? string.Empty : user.PhoneNumber ?? string.Empty;

            // Stop if the required contact information is missing
            if (string.IsNullOrWhiteSpace(contact))
            {
                AnsiConsole.MarkupLine("[red]✗ No contact information found for the saved 2FA method. Contact admin.[/]");
                Console.ReadKey(true);
                return;
            }

            // Inform the user where the verification code is being sent
            AnsiConsole.MarkupLine($"Sending code to: [blue]{contact}[/]");

            // Perform the verification using the existing TwoFactor class
            bool verified = TwoFactor.Verify(user.TwoFactorChoice, user.Email, user.PhoneNumber);

            // If verification fails, ask the user if they want to try again
            if (!verified)
            {
                AnsiConsole.MarkupLine("[red]✗ Verification failed.[/]\n");

                if (AnsiConsole.Confirm("Verification failed. Try again?"))
                    ForgotPassword(userStore);

                return;
            }

            // Verification successful
            AnsiConsole.MarkupLine("\n[green]✓ Verified![/]\n");

            // Loop until the user enters a valid new password
            while (true)
            {
                var newPass = AnsiConsole.Prompt(new TextPrompt<string>("Enter [green]new password[/]:").Secret());
                var confirm = AnsiConsole.Prompt(new TextPrompt<string>("Confirm:").Secret());

                // Check if passwords match
                if (newPass != confirm)
                {
                    AnsiConsole.MarkupLine("[red]✗ Passwords do not match![/]\n");
                    continue;
                }

                // Check if the password meets strength requirements
                if (!PasswordValidator.IsStrong(newPass))
                {
                    AnsiConsole.MarkupLine("[red]✗ Weak password! Must include: upper, lower, digit, special, 6+ chars[/]\n");
                    continue;
                }

                // Attempt to reset and save the new password
                if (user.ResetPassword(newPass))
                {
                    userStore.SaveToJson();
                    AnsiConsole.MarkupLine("[green]✓ Password reset successfully![/]");
                    Console.ReadKey(true);
                    return;
                }

                // If saving fails, allow retry
                AnsiConsole.MarkupLine("[red]✗ Failed to reset password. Try again.[/]");
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