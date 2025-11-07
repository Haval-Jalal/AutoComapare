using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

// Two-factor authentication options
public enum TwoFactorMethod
{
    Email,
    SMS
}

// Interface for 2FA sending
public interface ISecondFactorSender
{
    void SendEmailCode(string toEmail, string code);
    void SendSmsCode(string toPhone, string code);
}

// Interface for code generator
public interface ICodeGenerator
{
    string GenerateCode(int length);
}

// Numeric code generator for 2FA
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

// SMTP sender (reads info from .env)
public class SmtpSender : ISecondFactorSender
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _fromEmail;
    private readonly string _password;

    public SmtpSender(string email, string password, string host, int port)
    {
        _fromEmail = email;
        _password = password;
        _smtpHost = host;
        _smtpPort = port;
    }

    public void SendEmailCode(string toEmail, string code)
    {
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
        throw new NotImplementedException(); // Twilio to be implemented later
    }
}

// Helper class for password validation
public static class PasswordValidator
{
    public static bool IsStrong(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        if (password.Length < 6) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""{}|<>]")) return false;
        return true;
    }
}

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid(); // Unique ID
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime RegisteredAt { get; private set; }
    public TwoFactorMethod TwoFactorChoice { get; private set; }
    public string? Email { get; private set; }
    public string? PhoneNumber { get; private set; }
    public List<string> SearchHistory { get; } = new();

    // Temporary storage for 2FA code (hash) and expiration
    private string? _pending2faCodeHash;
    private DateTime? _pending2faExpiresAt;

    // Constructor
    public User() { }

    // Register a new user
    public bool Register(string username, string plainPassword, TwoFactorMethod method, string contactValue, List<User> allUsers)
    {
        // Check if username is already taken
        if (allUsers.Any(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine("Username already taken.");
            return false;
        }

        // Check password strength
        if (!PasswordValidator.IsStrong(plainPassword))
        {
            Console.WriteLine("Password is not strong enough.");
            return false;
        }

        Username = username.Trim();
        PasswordHash = Sha256(plainPassword); // Hash the password
        TwoFactorChoice = method;

        // Store 2FA contact info
        switch (method)
        {
            case TwoFactorMethod.Email:
                Email = contactValue.Trim();
                PhoneNumber = null;
                break;
            case TwoFactorMethod.SMS:
                PhoneNumber = contactValue.Trim();
                Email = null;
                break;
        }

        RegisteredAt = DateTime.UtcNow;
        return true;
    }

    // Login (sends 2FA code)
    public bool Login(string enteredPassword, ISecondFactorSender sender, ICodeGenerator generator)
    {
        if (!CheckPassword(enteredPassword))
        {
            Console.WriteLine("Wrong password.");
            return false;
        }

        // Send 2FA code
        SendTwoFactorCode(sender, generator, TimeSpan.FromMinutes(5));
        return true;
    }

    // Logout
    public void Logout()
    {
        Console.WriteLine($"{Username} logged out.");
    }

    // Delete account
    public void DeleteAccount(List<User> allUsers)
    {
        allUsers.Remove(this);
        Console.WriteLine($"{Username} account deleted.");
    }

    // Show search history
    public void GetHistory()
    {
        Console.WriteLine($"Search history for {Username}:");
        foreach (var item in SearchHistory)
            Console.WriteLine(item);
    }

    // Check password
    public bool CheckPassword(string enteredPassword)
    {
        return PasswordHash == Sha256(enteredPassword);
    }

    // Send 2FA code
    public void SendTwoFactorCode(ISecondFactorSender sender, ICodeGenerator generator, TimeSpan validity, int codeLength = 6)
    {
        string code = generator.GenerateCode(codeLength);

        if (TwoFactorChoice == TwoFactorMethod.Email)
        {
            if (string.IsNullOrWhiteSpace(Email)) throw new InvalidOperationException("Email not set.");
            sender.SendEmailCode(Email, code);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber)) throw new InvalidOperationException("Phone number not set.");
            sender.SendSmsCode(PhoneNumber, code);
        }

        _pending2faCodeHash = Sha256(code);
        _pending2faExpiresAt = DateTime.UtcNow.Add(validity);
    }

    // Verify 2FA code
    public bool VerifyTwoFactorCode(string code)
    {
        if (_pending2faCodeHash is null || _pending2faExpiresAt is null) return false;
        if (DateTime.UtcNow > _pending2faExpiresAt.Value) return false;

        bool ok = _pending2faCodeHash == Sha256(code);

        _pending2faCodeHash = null;
        _pending2faExpiresAt = null;

        return ok;
    }

    // SHA256 hash function for passwords and codes
    private static string Sha256(string input)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }
}