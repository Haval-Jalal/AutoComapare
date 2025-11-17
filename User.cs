using Microsoft.IdentityModel.Tokens;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Twilio; 
using Twilio.Rest.Api.V2010.Account;

namespace AutoCompare
{
    public class User
    {
        //Attribut
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public List<string> SearchHistory { get; set; }
        public string? TwoFactorCode { get; set; } = null;
        public DateTime? TwoFactorCodeExpiry { get; set; } = null;
        public DateTime? TwoFactorExpiry { get; set; } = null;
        public bool IsTwoFactorEnabled { get; set; } = false;
        public bool IsEmailEnabled { get; set; } = false;
        public bool IsPhoneEnabled { get; set; }
        public bool IsEmailVerified { get; set; } = false;
        public bool IsPhoneVerified { get; set; } = false;

        //Konstruktor
        public User(string username, string passwordHash, string phoneNumber, string email)
        {
            Username = username;
            PasswordHash = passwordHash;
            PhoneNumber = phoneNumber;
            Email = email;
            SearchHistory = new List<string>();
        }
        //Metod to Save user information to search history List user.json
        public void AddUserInfoToJson(string info)
        {
            string filePath = "users.json";
            var dataStore = new DataStore<User>();
            dataStore.LoadFromJson(filePath);
            var user = dataStore.FindItem(u => u.Username == this.Username);
            if (user != null)
            {
                user.SearchHistory.Add(info);
                dataStore.UpdateItem(user);
                dataStore.SaveToJson(filePath);
            }
            else
            {
                AnsiConsole.MarkupLine("[red]User not found in data store.[/]");
                //press Enter to To back to main menu
                AnsiConsole.MarkupLine("Press [green]Enter[/] to return to the main menu...");
            }
        }
        //Metod to toString override
        public override string ToString()
        {
            return $"User: {Username}, Email: {Email}, Phone: {PhoneNumber}, Search History Count: {SearchHistory.Count}";
        }
        //Metod to Hash password
        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (var b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        //Metod to validate email
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;
            try
            {
                // Use Regex to validate email format
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }
        }
        //Metod to validate phone number
        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;
            try
            {
                // Use Regex to validate phone number format (basic international format)
                string pattern = @"^\+?[1-9]\d{1,14}$";
                return Regex.IsMatch(phoneNumber, pattern);
            }
            catch (Exception)
            {
                return false;
            }
        }
        //Metod to generate 2FA code
        public static string GenerateTwoFactorCode()
        {
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            return code;
        }
        //Metod to send 2FA code via SMS using Twilio
        public void SendTwoFactorCodeViaSms()
        {
            if (string.IsNullOrEmpty(PhoneNumber))
                throw new InvalidOperationException("Phone number is not set.");

            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
                ?? throw new InvalidOperationException("TWILIO_ACCOUNT_SID not set.");
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
                ?? throw new InvalidOperationException("TWILIO_AUTH_TOKEN not set.");
            var fromPhone = Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER")
                ?? throw new InvalidOperationException("TWILIO_FROM_NUMBER not set.");

            TwilioClient.Init(accountSid, authToken);

            // Ensure TwoFactorCode exists
            if (string.IsNullOrEmpty(TwoFactorCode))
            {
                TwoFactorCode = GenerateTwoFactorCode();
                TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(10);
            }

            try
            {
                var message = MessageResource.Create(
                    body: $"Your verification code is: {TwoFactorCode}",
                    from: new Twilio.Types.PhoneNumber(fromPhone),
                    to: new Twilio.Types.PhoneNumber(PhoneNumber)
                );
                AnsiConsole.MarkupLine($"[green]📲 2FA code sent to {PhoneNumber}.[/]");
                IsTwoFactorEnabled = true;
            }
            catch (Twilio.Exceptions.ApiException ex)
            {
                // Log or surface a helpful message
                AnsiConsole.MarkupLine($"[red]Failed to send SMS: {ex.Message}[/]");
                throw;
            }
        }
        //Metod to verify 2FA code
        public bool VerifyTwoFactorCode(string code)
        {
            if (TwoFactorCodeExpiry == null || DateTime.Now > TwoFactorCodeExpiry)
            {
                return false; // Code expired
            }
            return code == TwoFactorCode;
        }
        //Metod to enable 2FA
        public void EnableTwoFactorAuthentication()
        {
            TwoFactorCode = GenerateTwoFactorCode();
            TwoFactorCodeExpiry = DateTime.UtcNow.AddMinutes(10);
            SendTwoFactorCodeViaSms();
        }
        //Metod to disable 2FA
        public void DisableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = false;
            AnsiConsole.MarkupLine("[yellow]⚠️ Two-Factor Authentication has been disabled.[/]");
        }
        //Metod to mark email as verified
        public void VerifyEmail()
        {
            IsEmailVerified = true;
            AnsiConsole.MarkupLine("[green]✅ Email has been verified.[/]");
        }
        //Metod to mark phone as verified
        public void VerifyPhone()
        {
            IsPhoneVerified = true;
            AnsiConsole.MarkupLine("[green]✅ Phone number has been verified.[/]");
        }
        //Metod to display user info
        public void DisplayUserInfo()
        {
            AnsiConsole.MarkupLine($"[bold]Username:[/] {Username}");
            AnsiConsole.MarkupLine($"[bold]Email:[/] {Email} {(IsEmailVerified ? "[green](Verified)[/]" : "[red](Not Verified)[/]")}");
            AnsiConsole.MarkupLine($"[bold]Phone Number:[/] {PhoneNumber} {(IsPhoneVerified ? "[green](Verified)[/]" : "[red](Not Verified)[/]")}");
            AnsiConsole.MarkupLine($"[bold]Two-Factor Authentication:[/] {(IsTwoFactorEnabled ? "[green]Enabled[/]" : "[red]Disabled[/]")}");
            AnsiConsole.MarkupLine($"[bold]Search History Count:[/] {SearchHistory.Count}");
        }
        public void DisplayPhoneNumber()
        {
            AnsiConsole.MarkupLine($"[bold]Phone Number:[/] {PhoneNumber} {(IsPhoneVerified ? "[green](Verified)[/]" : "[red](Not Verified)[/]")}");
            //press Enter to To back to main menu
            AnsiConsole.MarkupLine("Press [green]Enter[/] to return to the main menu...");
        }
        //Metod to Display Email
        public void DisplayEmail()
        {
            AnsiConsole.MarkupLine($"[bold]Email:[/] {Email} {(IsEmailVerified ? "[green](Verified)[/]" : "[red](Not Verified)[/]")}");
            //press Enter to To back to main menu
            AnsiConsole.MarkupLine("Press [green]Enter[/] to return to the main menu...");
        }
        //Metod to clear search history
        public void ClearSearchHistory()
        {
            SearchHistory.Clear();
            AnsiConsole.MarkupLine("[green]🗑️ Search history cleared.[/]");
        }
        //Metod to display search history
        public void DisplaySearchHistory()
        {
            if (SearchHistory.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]⚠️ No search history found.[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold]📜 Search History:[/]");
                for (int i = 0; i < SearchHistory.Count; i++)
                {
                    AnsiConsole.MarkupLine($"[blue]{i + 1}.[/] {SearchHistory[i]}");
                }
            }
        }
    }
}