using System;
using System.Security.Cryptography;
using System.Text;

namespace AutoCompare
{
    /// <summary>
    /// TwoFactorService handles generating numeric verification codes,
    /// sending via Email (SMTP) or SMS (Twilio), and verifying codes.
    /// Reads configuration from .env via DotNetEnv (if installed) or environment variables.
    /// </summary>
    public class TwoFactorService
    {
        private readonly string _smtpEmail;
        private readonly string _smtpPassword;
        private readonly string _smtpHost;
        private readonly int _smtpPort;

        private readonly string? _twilioSid;
        private readonly string? _twilioAuthToken;
        private readonly string? _twilioFromNumber;

        private readonly ICodeGenerator _generator;
        private readonly ISecondFactorSender _sender;

        public TwoFactorService()
        {
            try
            {
                DotNetEnv.Env.Load(); // Load .env from project root
                Console.WriteLine(".env file loaded successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to load .env: " + ex.Message);
            }

            // Read variables from environment
            _smtpEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL") ?? "";
            _smtpPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD") ?? "";
            _smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.gmail.com";
            _smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var p) ? p : 587;

            _twilioSid = Environment.GetEnvironmentVariable("TWILIO_SID") ?? "";
            _twilioAuthToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN") ?? "";
            _twilioFromNumber = Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER") ?? "";

            // Debug prints to check if values are loaded
            Console.WriteLine($"SMTP_EMAIL={_smtpEmail}");
            Console.WriteLine($"SMTP_PASSWORD={(string.IsNullOrEmpty(_smtpPassword) ? "(not set)" : "(set)")}");
            Console.WriteLine($"TWILIO_SID={_twilioSid}");
            Console.WriteLine($"TWILIO_AUTH_TOKEN={(string.IsNullOrEmpty(_twilioAuthToken) ? "(not set)" : "(set)")}");
            Console.WriteLine($"TWILIO_FROM_NUMBER={_twilioFromNumber}");
            
            _generator = new NumericCodeGenerator();
            _sender = new SmtpAndTwilioSender(_smtpEmail, _smtpPassword, _smtpHost, _smtpPort, _twilioSid, _twilioAuthToken, _twilioFromNumber);
        }
        /// <summary>
        /// Sends a 2FA code to the user. Returns codeHash and expiry.
        /// </summary>
        public (string codeHash, DateTime expiresAt) SendCode(User user, TimeSpan validity, int length = 6)
        {
            try
            {
                var code = _generator.GenerateCode(length);

                if (user.TwoFactorChoice == TwoFactorMethod.Email)
                {
                    if (string.IsNullOrWhiteSpace(user.Email))
                        throw new InvalidOperationException("User has no email configured.");
                    
                    bool sent = _sender.SendEmailCode(user.Email!, code);
                    if (sent) Console.WriteLine("A verification code has been sent to your email.");
                    else throw new Exception("Email sending failed.");
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                        throw new InvalidOperationException("User has no phone number configured.");
                    
                    bool sent = _sender.SendSmsCode(user.PhoneNumber!, code);
                    if (sent) Console.WriteLine("A verification code has been sent via SMS.");
                    else throw new Exception("SMS sending failed.");
                }

                var hash = Sha256(code);
                var expiresAt = DateTime.UtcNow.Add(validity);
                return (hash, expiresAt);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send 2FA code: {ex.Message}");
                return (string.Empty, DateTime.MinValue);
            }
        }

        /// <summary>
        /// Verify a provided code against stored codeHash and expiry.
        /// </summary>
        public bool VerifyCode(string enteredCode, string? codeHash, DateTime? expiresAt)
        {
            if (string.IsNullOrEmpty(codeHash) || expiresAt == null) return false;
            if (DateTime.UtcNow > expiresAt.Value) return false;
            return codeHash == Sha256(enteredCode);
        }

        private static string Sha256(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }
    }

    /// <summary>
    /// Numeric code generator for 2FA.
    /// </summary>
    public class NumericCodeGenerator : ICodeGenerator
    {
        private static readonly char[] Digits = "0123456789".ToCharArray();
        private readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();

        public string GenerateCode(int length)
        {
            var bytes = new byte[length];
            _rng.GetBytes(bytes);

            var sb = new StringBuilder(length);
            for (int i = 0; i < length; i++)
                sb.Append(Digits[bytes[i] % Digits.Length]);

            return sb.ToString();
        }
    }

    /// <summary>
    /// Sends Email via SMTP and SMS via Twilio.
    /// Returns bool to indicate success.
    /// </summary>
    public class SmtpAndTwilioSender : ISecondFactorSender
    {
        private readonly string _fromEmail;
        private readonly string _password;
        private readonly string _smtpHost;
        private readonly int _smtpPort;

        private readonly string? _twilioSid;
        private readonly string? _twilioAuthToken;
        private readonly string? _twilioFromNumber;

        public SmtpAndTwilioSender(string fromEmail, string password, string smtpHost, int smtpPort, string? twilioSid, string? twilioAuthToken, string? twilioFromNumber)
        {
            _fromEmail = fromEmail;
            _password = password;
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _twilioSid = twilioSid;
            _twilioAuthToken = twilioAuthToken;
            _twilioFromNumber = twilioFromNumber;
        }

        public bool SendEmailCode(string toEmail, string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_fromEmail)) throw new InvalidOperationException("SMTP_EMAIL is not set.");
                if (string.IsNullOrWhiteSpace(toEmail)) throw new InvalidOperationException("Recipient email is not set.");

                using var message = new System.Net.Mail.MailMessage(_fromEmail, toEmail)
                {
                    Subject = "AutoCompare verification code",
                    Body = $"Your verification code is: {code}"
                };
                using var client = new System.Net.Mail.SmtpClient(_smtpHost, _smtpPort)
                {
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential(_fromEmail, _password)
                };
                client.Send(message);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                return false;
            }
        }

        public bool SendSmsCode(string toPhone, string code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_twilioSid) || string.IsNullOrWhiteSpace(_twilioAuthToken) || string.IsNullOrWhiteSpace(_twilioFromNumber))
                    throw new InvalidOperationException("Twilio configuration is not set.");

                Twilio.TwilioClient.Init(_twilioSid, _twilioAuthToken);
                Twilio.Rest.Api.V2010.Account.MessageResource.Create(
                    body: $"Your verification code is: {code}",
                    from: new Twilio.Types.PhoneNumber(_twilioFromNumber),
                    to: new Twilio.Types.PhoneNumber(toPhone)
                );
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send SMS: {ex.Message}");
                return false;
            }
        }
    }

    // Supporting interfaces
    public interface ISecondFactorSender
    {
        bool SendEmailCode(string toEmail, string code);
        bool SendSmsCode(string toPhone, string code);
    }

    public interface ICodeGenerator { string GenerateCode(int length); }

    public enum TwoFactorMethod { Email, SMS }
}