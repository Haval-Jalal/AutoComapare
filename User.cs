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
        Email,
        SMS
    }

    // Interface that defines methods for sending 2FA codes
    public interface ISecondFactorSender
    {
        void SendEmailCode(string toEmail, string code);
        void SendSmsCode(string toPhone, string code);
    }

    // Interface for code generator
    public interface ICodeGenerator
    {
        string GenerateCode(int length);    // Generates a random code with the given number of digits
    }

    // Class that generates numeric 2FA codes
    public class NumericCodeGenerator : ICodeGenerator
    {
        private static readonly char[] Digits = "0123456789".ToCharArray(); // 0-9 digits
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create(); // Cryptographically secure RNG

        public string GenerateCode(int length)
        {
            var bytes = new byte[length];  // Create a byte array to hold random data
            _rng.GetBytes(bytes);     // Fill the array with secure random bytes

            var sb = new StringBuilder(length);   // Use StringBuilder for efficient string construction
            for (int i = 0; i < length; i++)
                sb.Append(Digits[bytes[i] % Digits.Length]);   // Map random byte to one of the 10 digits (0–9)

            return sb.ToString();   // Return the final numeric code as a string
        }
    }

    // Class that sends 2FA codes using an SMTP email server + Twilio for SMS
    public class SmtpSender : ISecondFactorSender
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _fromEmail;
        private readonly string _password;

        private readonly string? _twilioSid;
        private readonly string? _twilioAuthToken;
        private readonly string? _twilioFromNumber;

        public SmtpSender(string email, string password, string host, int port, string? twilioSid, string? twilioAuthToken, string? twilioFromNumber)
        {
            _fromEmail = email;
            _password = password;
            _smtpHost = host;
            _smtpPort = port;

            _twilioSid = twilioSid;
            _twilioAuthToken = twilioAuthToken;
            _twilioFromNumber = twilioFromNumber;
        }

        public void SendEmailCode(string toEmail, string code)
        {
            // Check that sender and recipient emails are set
            if (string.IsNullOrWhiteSpace(_fromEmail))
                throw new InvalidOperationException("SMTP_FROM email is not set.");
            if (string.IsNullOrWhiteSpace(toEmail))
                throw new InvalidOperationException("Recipient email is not set.");


            // Create the email message to send
            using var message = new System.Net.Mail.MailMessage(_fromEmail, toEmail)
            {
                Subject = "Your 2FA Code",
                Body = $"Your verification code is: {code}"
            };
            using var client = new System.Net.Mail.SmtpClient(_smtpHost, _smtpPort)
            {
                EnableSsl = true,
                Credentials = new System.Net.NetworkCredential(_fromEmail, _password)
            };
            client.Send(message);
        }

        public void SendSmsCode(string toPhone, string code)
        {
            // Check that sender and recipient sms settings are set
            if (string.IsNullOrWhiteSpace(toPhone))
               throw new InvalidOperationException("Recipient phone number is not set.");

            TwilioClient.Init(_twilioSid, _twilioAuthToken);
            var message = MessageResource.Create(
                body: $"Your verification code is: {code}",
                from: new Twilio.Types.PhoneNumber(_twilioFromNumber),
                to: new Twilio.Types.PhoneNumber(toPhone)
            );
        }
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

        private string? _pending2faCodeHash;
        private DateTime? _pending2faExpiresAt;

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
                Email = contact.Trim();
            else
                PhoneNumber = contact.Trim();

            RegisteredAt = DateTime.UtcNow;
            return true;
        }

        // LOGIN
        public bool Login(string enteredPassword, ISecondFactorSender sender, ICodeGenerator generator)
        {
            if (!CheckPassword(enteredPassword))
            {
                Console.WriteLine("Wrong password.");
                return false;
            }
            // If user has BOTH an email and phone number → allow choice
            if (Email != null && PhoneNumber != null)
            {
                Console.WriteLine("Choose verification method:");
                Console.WriteLine("1. Email");
                Console.WriteLine("2. SMS");
                Console.Write("Your choice: ");

                var choice = Console.ReadLine();

                if (choice == "1")
                    TwoFactorChoice = TwoFactorMethod.Email;
                else if (choice == "2")
                    TwoFactorChoice = TwoFactorMethod.SMS;
                else
                {
                    Console.WriteLine("Invalid choice.");
                    return false;
                }
            }
            else if (Email != null)
            {
                TwoFactorChoice = TwoFactorMethod.Email;
            }
            else if (PhoneNumber != null)
            {
                TwoFactorChoice = TwoFactorMethod.SMS;
            }
            else
            {
                Console.WriteLine("User has neither email nor phone number configured.");
                return false;
            }

            SendTwoFactorCode(sender, generator, TimeSpan.FromMinutes(5));
            return true;
        }

        // FORGOT PASSWORD
        public void ForgotPassword(ISecondFactorSender sender, ICodeGenerator generator)
        {
            SendTwoFactorCode(sender, generator, TimeSpan.FromMinutes(10));
            Console.WriteLine("A reset code has been sent to your 2FA method.");
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

        public void SendTwoFactorCode(ISecondFactorSender sender, ICodeGenerator generator, TimeSpan validity, int len = 6)
        {
            string code = generator.GenerateCode(len);

            if (TwoFactorChoice == TwoFactorMethod.Email)
                sender.SendEmailCode(Email!, code);
            else
                sender.SendSmsCode(PhoneNumber!, code);

            _pending2faCodeHash = Sha256(code);
            _pending2faExpiresAt = DateTime.UtcNow.Add(validity);
        }

        public bool VerifyTwoFactorCode(string code)
        {
            if (_pending2faCodeHash is null || _pending2faExpiresAt is null) return false;
            if (DateTime.UtcNow > _pending2faExpiresAt.Value) return false;

            bool ok = _pending2faCodeHash == Sha256(code);
            _pending2faCodeHash = null;
            _pending2faExpiresAt = null;

            return ok;
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