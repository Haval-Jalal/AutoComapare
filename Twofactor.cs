using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using Spectre.Console;
using Twilio;
using Twilio.Rest.Verify.V2.Service;
using DotNetEnv;

public enum TwoFactorMethod
{
    none,
    Email,
    SMS
}

public class TwoFactor
{
    public static bool Verify(TwoFactorMethod method, string? email = null, string? phoneNumber = null)
    {
        switch (method)
        {
            case TwoFactorMethod.none:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: No verification selected. Access granted.");
                Console.ResetColor();
                return true;

            case TwoFactorMethod.Email:
                if (string.IsNullOrWhiteSpace(email))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("2FA: Email is missing. Cannot verify.");
                    Console.ResetColor();
                    return false;
                }
                return EmailVerification(email);

            case TwoFactorMethod.SMS:
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("2FA: Phone number is missing. Cannot verify.");
                    Console.ResetColor();
                    return false;
                }
                return SMSVerification(phoneNumber);

            default:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: Unknown verification method.");
                Console.ResetColor();
                return false;
        }
    }

    private static bool EmailVerification(string email)
    {
        try
        {
            Env.Load(Path.Combine(AppContext.BaseDirectory, ".env")); // Läser .env

            string fromEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "";
            string password = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            string host = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            int port = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;

            var random = new Random();
            string code = random.Next(100000, 999999).ToString();

            var smtp = new SmtpClient(host)
            {
                Port = port,
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl = true
            };

            var message = new MailMessage(fromEmail, email)
            {
                Subject = "AutoCompare Two-Factor Verification",
                Body = $"Hello!\n\nYour verification code is: {code}\n\nStay safe.\n– AutoCompare"
            };

            smtp.Send(message);

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"2FA: A verification code has been sent to {email}");
            Console.ResetColor();

            Console.Write("Enter the verification code: ");
            string input = Console.ReadLine()?.Trim() ?? "";

            if (input == code)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: Verification successful. Access granted!");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: Incorrect verification code. Access denied.");
                Console.ResetColor();
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"2FA: Error sending email: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            Console.ResetColor();
            return false;
        }
    }

    private static bool SMSVerification(string phoneNumber)
    {
        try
        {
            Env.Load();

            string accountSid = Environment.GetEnvironmentVariable("TWILIO_SID") ?? "";
            string authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? "";
            string verifySid = Environment.GetEnvironmentVariable("TWILIO_VERIFY_SID") ?? "";

            if (string.IsNullOrWhiteSpace(accountSid) ||
                string.IsNullOrWhiteSpace(authToken) ||
                string.IsNullOrWhiteSpace(verifySid))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: Twilio credentials missing. Cannot send SMS.");
                Console.ResetColor();
                return false;
            }

            TwilioClient.Init(accountSid, authToken);

            var verification = VerificationResource.Create(
                to: phoneNumber,
                channel: "sms",
                pathServiceSid: verifySid
            );

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"2FA: Code sent to {phoneNumber}");
            Console.ResetColor();

            Console.Write("Enter the verification code: ");
            string code = Console.ReadLine()?.Trim() ?? "";

            var check = VerificationCheckResource.Create(
                to: phoneNumber,
                code: code,
                pathServiceSid: verifySid
            );

            if (check.Status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: Phone number verified. Access granted!");
                Console.ResetColor();
                return true;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("2FA: Wrong code. Access denied.");
                Console.ResetColor();
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"2FA: Error sending SMS: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }
}