using System;

namespace AutoCompare
{
    /// <summary>
    /// UIManager handles the main menu and user flows.
    /// It uses your existing DataStore<User> and TwoFactorService.
    /// All console messages are in English.
    /// </summary>
    public class UIManager
    {
        private readonly TwoFactorService _twoFactor;
        private readonly DataStore<User> _userStore;

        public UIManager()
        {
            _twoFactor = new TwoFactorService();
            _userStore = new DataStore<User>(); // uses your existing DataStore<T> implementation
        }

        public void Start()
        {
            Console.WriteLine("===================================");
            Console.WriteLine("  Welcome to AutoCompare");
            Console.WriteLine("  A friendly assistant to compare cars for people who don't know much about cars.");
            Console.WriteLine("===================================\n");

            bool running = true;
            while (running)
            {
                Console.WriteLine("\nMain menu:");
                Console.WriteLine("1) Register");
                Console.WriteLine("2) Login");
                Console.WriteLine("3) Exit");
                Console.Write("Choice: ");
                var choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        var created = User.Register(_userStore, _twoFactor);
                        if (created != null)
                            Console.WriteLine("Registration completed.");
                        break;
                    case "2":
                        var user = User.Login(_userStore, _twoFactor);
                        if (user != null)
                            ShowUserMenu(user);
                        break;
                    case "3":
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }

            Console.WriteLine("Goodbye!");
        }

        private void ShowUserMenu(User user)
        {
            bool loggedIn = true;
            while (loggedIn)
            {
                Console.WriteLine($"\nHello {user.Username} — choose an option:");
                Console.WriteLine("1) Profile settings (reset password, delete account, change 2FA method)");
                Console.WriteLine("2) Logout");
                Console.Write("Choice: ");
                var c = Console.ReadLine()?.Trim();

                switch (c)
                {
                    case "1":
                        ProfileSettings(user);
                        break;
                    case "2":
                        Console.WriteLine("Logging out...");
                        loggedIn = false;
                        break;
                    default:
                        Console.WriteLine("Invalid choice.");
                        break;
                }
            }
        }

        private void ProfileSettings(User user)
        {
            Console.WriteLine("\nProfile settings:");
            Console.WriteLine("1) Reset password");
            Console.WriteLine("2) Delete account");
            Console.WriteLine("3) Change 2FA method");
            Console.Write("Choice: ");
            var c = Console.ReadLine()?.Trim();

            switch (c)
            {
                case "1":
                    user.ForgotPassword(_userStore, _twoFactor);
                    break;
                case "2":
                    Console.Write("Are you sure you want to delete your account? (y/n): ");
                    if (Console.ReadLine()?.Trim().ToLower() == "y")
                        user.DeleteAccount(_userStore);
                    break;
                case "3":
                    ChangeTwoFactor(user);
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        }

        private void ChangeTwoFactor(User user)
        {
            Console.WriteLine("Choose new 2FA method: 1) Email  2) SMS");
            var choice = Console.ReadLine()?.Trim();
            if (choice == "1")
            {
                Console.Write("Enter new email: ");
                var email = Console.ReadLine()?.Trim() ?? "";
                user.Email = email;
                user.TwoFactorChoice = TwoFactorMethod.Email;
                _userStore.SaveToJson("users.json");
                Console.WriteLine("2FA method changed to Email.");
            }
            else if (choice == "2")
            {
                Console.Write("Enter phone number (international format): ");
                var phone = Console.ReadLine()?.Trim() ?? "";
                user.PhoneNumber = phone;
                user.TwoFactorChoice = TwoFactorMethod.SMS;
                _userStore.SaveToJson("users.json");
                Console.WriteLine("2FA method changed to SMS.");
            }
            else
            {
                Console.WriteLine("Invalid choice.");
            }
        }
    }
}