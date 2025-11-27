using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AutoCompare
{
    // Enkel hjälparklass för att bedöma lösenordsstyrka
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
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid(); //json ska deserialisera
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public TwoFactorMethod TwoFactorChoice { get; set; } = TwoFactorMethod.Email;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public List<string> SearchHistory { get; set; } = new List<string>();

        public User() { }


        public static string ReadHiddenPassword() //ersätter lösenordet med *
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
       
        public static bool IsValidEmail(string email) //formatkontroll för e-post
        {
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);
        }


        //Register sätter bara properties, lagring hanteras av caller/userStore
        public bool Register(string username, string plainPassword, TwoFactorMethod method, string contact)
        {
            if (!PasswordValidator.IsStrong(plainPassword)) //Kontrollerar lösenordsstyrka
            {
                Console.WriteLine("Password is not strong enough, try again!");
                return false;
            }

            if (!IsValidEmail(username)) //Kontrollerar att username är giltig e-post
            {
                Console.WriteLine("Invalid email format!");
                return false;
            }

            // Om 2FA via email, kontrollera att kontakt är giltig e-post
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

        // Kontrollerar inmatat lösenord mot sparad hash
        public bool CheckPassword(string enteredPassword)
        {
            return PasswordHash == Sha256(enteredPassword);
        }

        // DeleteAccount tar emot userStore och ber store ta bort och spara
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

        // FORGOT PASSWORD — flöde för att nollställa lösenord via användarens sparade 2FA
        public void ForgotPassword(DataStore<User> userStore)
        {
            // Visa rubrik och layout
            Console.Clear();
            AnsiConsole.Write(new FigletText("Forgot Password").Centered().Color(Color.Yellow));
            AnsiConsole.Write(new Rule("[yellow]Password Reset[/]").RuleStyle("grey").Centered());
            AnsiConsole.WriteLine();

            // Fråga efter email och hitta användaren i store
            string username = AnsiConsole.Ask<string>("Enter your [green]email[/]:").Trim();
            var user = userStore.List.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));

            // Om ingen användare hittas, erbjud val
            if (user == null)
            {
                AnsiConsole.MarkupLine($"\n[red]✗ No account found:[/] [bold]{username}[/]");
                var action = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Would you like to register?")
                    .AddChoices(new[] { "Try again", "Register", "Cancel" }));

                if (action == "Register")
                {
                    // Informera om att använda registreringsmenyn
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

            // Bekräftar att konto finns och visa 2FA-info
            AnsiConsole.MarkupLine($"[green]✓ Account found:[/] [bold]{username}[/]");
            AnsiConsole.MarkupLine("[yellow]Verify your identity with your registered 2FA method.[/]\n");

            // Kontrollerar att användaren har en giltig 2FA-metod
            if (user.TwoFactorChoice != TwoFactorMethod.Email && user.TwoFactorChoice != TwoFactorMethod.SMS)
            {
                AnsiConsole.MarkupLine("[red]✗ No 2FA method registered on this account. Contact admin for help.[/]");
                Console.ReadKey(true);
                return;
            }

            // Välj kontakt beroende på 2FA-metod
            bool isEmailMethod = user.TwoFactorChoice == TwoFactorMethod.Email;
            string contact = isEmailMethod ? user.Email ?? string.Empty : user.PhoneNumber ?? string.Empty;

            // Avbryt om kontakt saknas
            if (string.IsNullOrWhiteSpace(contact))
            {
                AnsiConsole.MarkupLine("[red]✗ No contact information found for the saved 2FA method. Contact admin.[/]");
                Console.ReadKey(true);
                return;
            }

            AnsiConsole.MarkupLine($"Sending code to: [blue]{contact}[/]");

            // Verifierar med hjälp av TwoFactor-klassen
            bool verified = TwoFactor.Verify(user.TwoFactorChoice, user.Email, user.PhoneNumber);

            // Om verifiering misslyckas, erbjuder att försöka igen
            if (!verified)
            {
                AnsiConsole.MarkupLine("[red]✗ Verification failed.[/]\n");

                if (AnsiConsole.Confirm("Verification failed. Try again?"))
                    ForgotPassword(userStore);

                return;
            }

            // Verifiering lyckades
            AnsiConsole.MarkupLine("\n[green]✓ Verified![/]\n");

            //Loop för att sätta nytt lösenord tills det är giltigt och sparas
            while (true)
            {
                var newPass = AnsiConsole.Prompt(new TextPrompt<string>("Enter [green]new password[/]:").Secret());
                var confirm = AnsiConsole.Prompt(new TextPrompt<string>("Confirm:").Secret());

                // kontrollerar om lösenord matchar
                if (newPass != confirm)
                {
                    AnsiConsole.MarkupLine("[red]✗ Passwords do not match![/]\n");
                    continue;
                }

                // Kontrollerar styrkan
                if (!PasswordValidator.IsStrong(newPass))
                {
                    AnsiConsole.MarkupLine("[red]✗ Weak password! Must include: upper, lower, digit, special, 6+ chars[/]\n");
                    continue;
                }

                // Försök nollställa lösenord och sparar 
                if (user.ResetPassword(newPass))
                {
                    userStore.SaveToJson();
                    AnsiConsole.MarkupLine("[green]✓ Password reset successfully![/]");
                    Console.ReadKey(true);
                    return;
                }

                // försök igen om sparning misslyckas 
                AnsiConsole.MarkupLine("[red]✗ Failed to reset password. Try again.[/]");
            }
        }

        // nytt lösenord (kontrollerar styrka och uppdaterar hash)
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
        
        // Hashfunktion för SHA-256
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
    }
}