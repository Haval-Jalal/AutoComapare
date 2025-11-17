using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AutoCompare
{
    public static class UIManager
    {
        // Displays a welcome message to the user
        public static void DisplayWelcomeMessage()
        {
            AnsiConsole.Write(new Rule("[bold green]🚗 AutoCompare Tool 🚗[/]").RuleStyle("green").Centered());

            AnsiConsole.MarkupLine("[bold green]👋 Welcome to AutoCompare![/]");
            AnsiConsole.MarkupLine("🗂️ This tool helps you [bold]compare files and directories[/] with ease.");
            AnsiConsole.MarkupLine("✨ Let's get started and make your workflow smoother!\n");

            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("green"))
                .Start("🔄 [green]Loading modules and preparing the workspace...[/]", ctx =>
                {
                    Thread.Sleep(3000); // Simulate loading
                });
            var header = new FigletText("AutoCompare")
                .Centered()
                .Color(Color.Blue3);
            AnsiConsole.Write(header);
            Console.ReadLine();
            AnsiConsole.MarkupLine("[bold green]✅ Ready to go![/]\n");
        }
        // Displays a goodbye message to the user same style as welcome message in centered
        public static void DisplayGoodbyeMessage()
        {
            AnsiConsole.MarkupLine("\n[bold green]👋 Thank you for using AutoCompare![/]");
            AnsiConsole.MarkupLine("🚀 We hope it made your file and directory comparisons easier.");
            AnsiConsole.MarkupLine("🌟 Have a great day ahead!\n");
            AnsiConsole.Write(new Rule("[bold green]🚗 AutoCompare Tool - Goodbye! 🚗[/]").RuleStyle("green").Centered());
        }
        // Displays an error message to the user
        public static void DisplayErrorMessage(string message)
        {
            AnsiConsole.MarkupLine("\n[bold red]❌ Error:[/]");
            AnsiConsole.MarkupLine($"[red]{message}[/]");
            AnsiConsole.Write(new Rule("[bold red]🚗 AutoCompare Tool - Error 🚗[/]").RuleStyle("red").Centered());
        }
        // Displays a success message to the user
        public static void DisplaySuccessMessage(string message)
        {
            AnsiConsole.MarkupLine("\n[bold green]✅ Success:[/]");
            AnsiConsole.MarkupLine($"[green]{message}[/]");
            AnsiConsole.Write(new Rule("[bold green]🚗 AutoCompare Tool - Success 🚗[/]").RuleStyle("green").Centered());
        }
        // Displays an informational message to the user
        public static void DisplayInfoMessage(string message)
        {
            AnsiConsole.MarkupLine("\n[bold blue]ℹ️ Info:[/]");
            AnsiConsole.MarkupLine($"[blue]{message}[/]");
            AnsiConsole.Write(new Rule("[bold blue]🚗 AutoCompare Tool - Info 🚗[/]").RuleStyle("blue").Centered());
        }
        // Displays a loading message to the user
        public static void DisplayLoadingMessage(string message, int duration = 2000)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("green"))
                .Start($"🔄 [green]{message}[/]", ctx =>
                {
                    Thread.Sleep(duration); // Simulate loading
                });
        }
        // Displays a prompt message to the user and returns the input
        public static string DisplayPrompt(string message)
        {
            return AnsiConsole.Ask<string>($"[bold yellow]🔍 {message}[/]");
        }
        // Displays a confirmation message to the user and returns the input
        public static bool DisplayConfirmation(string message)
        {
            return AnsiConsole.Confirm($"[bold yellow]❓ {message}[/]");

        }
        // Displays a table with given title and data
        public static void DisplayTable(string title, List<string[]> data)
        {
            var table = new Table();
            table.AddColumn(new TableColumn(title).Centered());

            foreach (var row in data)
            {
                table.AddRow(row);
            }

            AnsiConsole.Write(table);
        }
        // Displays a menu with given title and options, returns the selected option
        public static string DisplayMenu(string title, List<string> options)
        {
            AnsiConsole.MarkupLine($"\n[bold yellow]📋 {title}[/]");
            var selectedOption = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[bold yellow]❓ Select an option:[/]")
                    .AddChoices(options)
            );

            return selectedOption;
        }
        // Displays a progress bar with given task name and duration
        public static void DisplayProgressBar(string taskName, int duration = 3000)
        {
            AnsiConsole.Progress()
                .Start(ctx =>
                {
                    var task = ctx.AddTask($"[green]{taskName}[/]");
                    while (!task.IsFinished)
                    {
                        task.Increment(1.0 / (duration / 100));
                        Thread.Sleep(100);
                    }
                });

        }
        // Displays a banner message with given text
        public static void DisplayBanner(string message)
        {
            AnsiConsole.Write(
                new FigletText(message)
                    .Centered()
                    .Color(Color.Green));
        }
        // Displays a notification message with given text
        public static void DisplayNotification(string message)
        {
            AnsiConsole.MarkupLine("\n[bold cyan]🔔 Notification:[/]");
            AnsiConsole.MarkupLine($"[cyan]{message}[/]");
            AnsiConsole.Write(new Rule("[bold cyan]🚗 AutoCompare Tool - Notification 🚗[/]").RuleStyle("cyan").Centered());
        }
        // Displays a divider line
        public static void DisplayDivider()
        {
            AnsiConsole.Write(new Rule().RuleStyle("gray").Centered());
        }
        // Displays a loading spinner with given message and duration
        public static void DisplayLoadingSpinner(string message, int duration = 2000)
        {
            AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots2)
                .SpinnerStyle(Style.Parse("green"))
                .Start($"🔄 [green]{message}[/]", ctx =>
                {
                    Thread.Sleep(duration); // Simulate loading
                });
        }
        // Displays a highlighted message with given text
        public static void DisplayHighlightedMessage(string message)
        {
            AnsiConsole.MarkupLine($"\n[bold yellow]🌟 {message} 🌟[/]");
        }
        // Displays a quote message with given text
        public static void DisplayQuoteMessage(string message)
        {
            AnsiConsole.MarkupLine($"\n[italic blue]❝ {message} ❞[/]");
        }
        // Displays a warning message with given text
        public static void DisplayWarningMessage(string message)
        {
            AnsiConsole.MarkupLine($"\n[bold red]⚠️ {message}[/]");
        }
        // Displays a checklist with given title and options, returns the selected options
        public static List<string> DisplayChecklist(string title, List<string> options)
        {
            AnsiConsole.MarkupLine($"\n[bold yellow]📋 {title}[/]");
            var selectedOptions = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title($"[bold yellow]❓ Select options:[/]")
                    .NotRequired() // Allow no selection
                    .AddChoices(options)
            );
            return selectedOptions;
        }
        // Displays a tree structure with given title and nodes
        public static void DisplayTree(string title, Dictionary<string, List<string>> nodes)
        {
            var tree = new Tree(title);
            foreach (var node in nodes)
            {
                var branch = tree.AddNode(node.Key);
                foreach (var child in node.Value)
                {
                    branch.AddNode(child);
                }
            }
            AnsiConsole.Write(tree);
        }
        // Displays a calendar with given month and year
        public static void DisplayCalendar(int month, int year)
        {
            var calendar = new Calendar(year, month);
            AnsiConsole.Write(calendar);
        }
        // Displays a bar chart with given title and data
        public static void DisplayBarChart(string title, Dictionary<string, int> data)
        {
            var chart = new BarChart()
                .Width(60)
                .Label("[bold green]" + title + "[/]")
                .CenterLabel();
            foreach (var item in data)
            {
                chart.AddItem(item.Key, item.Value, Color.Green);
            }
            AnsiConsole.Write(chart);
        }
        public static string GenerateCode() => Guid.NewGuid().ToString().Substring(0, 6);

        public static async Task SendCodeAsync(string email, string phone, string code)
        {
            if (!string.IsNullOrEmpty(email))
                await EmailService.SendAsync(email, "🔑 Your Activation Code", $"Code: {code}");
            else if (!string.IsNullOrEmpty(phone))
                await SmsService.SendAsync(phone, $"🔑 Your Activation Code: {code}");
        }


        //2FA send to email or sms based on user selection during registration dont show the code in the console
        public static async Task SendTwoFactorCode(string method, string contactInfo, string code)
        {
            if (method == "SMS")
            {
                var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
                 ?? throw new InvalidOperationException("TWILIO_ACCOUNT_SID not set.");
                var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
                 ?? throw new InvalidOperationException("TWILIO_AUTH_TOKEN not set.");
                TwilioClient.Init(accountSid, authToken);

                var message = MessageResource.Create(
                    body: $"Your verification code is: {code}",
                    from: new Twilio.Types.PhoneNumber(Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER")!),
                    to: new Twilio.Types.PhoneNumber(contactInfo)
                );

                AnsiConsole.MarkupLine("[grey]📤 SMS sent via Twilio.[/]");
            }
            else if (method == "Email")
            {

            }
            else
            {
                throw new InvalidOperationException("Invalid method for sending 2FA code.");
            }


        }
        //In real application, the code would be sent to the user's phone or email
        public static bool VerifyTwoFactorCode(string inputCode, string actualCode)
        {
            return inputCode == actualCode;
        }

        //Method to register new user with username and password and select 2fa option either sms or email 
        //use User class attributes Username, Password, TwoFactorMethod 
        public static void RegisterUser(User user)
        {
            Console.Clear();
            var header = new FigletText("User Registration")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);

            AnsiConsole.MarkupLine("\n[bold green]📝 User Registration[/]");

            if (!AnsiConsole.Confirm("Do you want to register a new user?"))
            {
                AnsiConsole.MarkupLine("[bold yellow]⚠️ Registration Cancelled![/]");
                return;
            }

            user.Username = AnsiConsole.Ask<string>("Enter Username:");
            var password = AnsiConsole.Ask<string>("Enter Password:");
            user.PasswordHash = User.HashPassword(password);

            user.PhoneNumber = AnsiConsole.Ask<string>("Enter Phone Number (optional):");
            user.Email = AnsiConsole.Ask<string>("Enter Email (optional):");
            var twoFactorOption = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select Two-Factor Authentication Method:")
                    .AddChoices(new[] { "SMS", "Email", "None" })
            );
            switch (twoFactorOption)
            {
                case "SMS":
                    RegisterWithSms2FA(user);
                    break;
                case "Email":
                    RegisterWithEmail2FA(user);
                    break;
                case "None":
                    AnsiConsole.MarkupLine("[bold yellow]⚠️ Two-Factor Authentication Disabled![/]");
                    break;
            }
        }

        private static void RegisterWithSms2FA(User user)
        {
            string phone;
            do
            {
                phone = AnsiConsole.Ask<string>("Enter Phone Number:");
                if (!User.IsValidPhoneNumber(phone))
                {
                    AnsiConsole.MarkupLine("[bold red]❌ Invalid Phone Number! Please try again.[/]");
                }
            }
            while (!User.IsValidPhoneNumber(phone));
            {
                user.PhoneNumber = phone;
                user.EnableTwoFactorAuthentication();
                string code = User.GenerateTwoFactorCode();
                // 📱 Replace this with actual SMS integration
                SendTwoFactorCode("SMS", user.PhoneNumber, code);
                AnsiConsole.MarkupLine("[bold green]✅ User Registered Successfully with SMS 2FA![/]");
            }
        }

        private static void RegisterWithEmail2FA(User user)
        {
            string mail;
            do
            {
                mail = AnsiConsole.Ask<string>("Enter Email:");
                if (!User.IsValidEmail(mail))
                {
                    AnsiConsole.MarkupLine("[bold red]❌ Invalid Email! Please try again.[/]");
                }
            }
            while (!User.IsValidEmail(mail));
            {
                user.Email = mail;
                user.EnableTwoFactorAuthentication();
                string code = User.GenerateTwoFactorCode();
                // 📧 Replace this with actual Email integration
                SendTwoFactorCode("Email", user.Email, code);
                AnsiConsole.MarkupLine("[bold green]✅ User Registered Successfully with Email 2FA![/]");
            }
        }
        //Method to login user with username and password and verify 2fa code
        public static bool LoginUser(User user)
        {
            Console.Clear();
            var header = new FigletText("User Login")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            AnsiConsole.MarkupLine("\n[bold green]🔐 User Login[/]");
            var username = AnsiConsole.Ask<string>("Enter Username:");
            var password = AnsiConsole.Ask<string>("Enter Password:");
            //if (user.Username != username || user.PasswordHash != User.HashPassword(password))
            //{
            //    AnsiConsole.MarkupLine("[bold red]❌ Invalid Username or Password![/]");
            //    return false;
            //}
            //var inputCode = AnsiConsole.Ask<string>("Enter Two-Factor Authentication Code:");
            ////In real application, the code would be sent to the user's phone or email
            //var actualCode = "123456"; //Simulated code for demonstration
            //if (!VerifyTwoFactorCode(inputCode, actualCode))
            //{
            //    AnsiConsole.MarkupLine("[bold red]❌ Invalid Two-Factor Code![/]");
            //    return false;
            //}
            AnsiConsole.MarkupLine("[bold green]✅ Login Successful![/]");
            return true;
        }
        //Method to display user profile information
        public static void DisplayUserProfile(User user)
        {
            Console.Clear();
            var  header = new FigletText("User Profile")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);

            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }

            AnsiConsole.MarkupLine("\n[bold green]👤 User Profile[/]");
            AnsiConsole.MarkupLine($"[bold]Username:[/] {user.Username}");
            AnsiConsole.MarkupLine($"[bold]Email:[/] {user.Email}");
            AnsiConsole.MarkupLine($"[bold]Phone Number:[/] {user.PhoneNumber}");
            AnsiConsole.MarkupLine($"[bold]Two-Factor Enabled:[/] {(user.IsTwoFactorEnabled ? "Enabled" : "Disabled")}");
            Console.ReadLine();
            MainMenu(user);
            return;
        }
        //Method to update user profile information
        public static void UpdateUserProfile(User user)
        {
            Console.Clear();
            var header = new FigletText("Update Profile")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);

            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                AnsiConsole.MarkupLine("\n[bold green]✏️ Update User Profile[/]");
                user.Email = AnsiConsole.Ask<string>("Enter New Email:");
                user.PhoneNumber = AnsiConsole.Ask<string>("Enter New Phone Number:");
                AnsiConsole.MarkupLine("[bold green]✅ Profile Updated Successfully![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
        }
        //Method to change user password
        public static void ChangeUserPassword(User user)
        {
            Console.Clear();
            var header = new FigletText("Change Password")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);

            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                AnsiConsole.MarkupLine("\n[bold green]🔒 Change Password[/]");
                var currentPassword = AnsiConsole.Ask<string>("Enter Current Password:");
                if (user.PasswordHash != User.HashPassword(currentPassword))
                {
                    AnsiConsole.MarkupLine("[bold red]❌ Invalid Current Password![/]");
                    Console.ReadLine();
                    MainMenu(user);
                    return;
                }
                var newPassword = AnsiConsole.Ask<string>("Enter New Password:");
                user.PasswordHash = User.HashPassword(newPassword);
                AnsiConsole.MarkupLine("[bold green]✅ Password Changed Successfully![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }

        }
        //Method to delete user account
        public static void ChangePassword(User user)
        {
            AnsiConsole.MarkupLine("\n[bold green]🗑️ Delete User Account[/]");
            var confirmation = AnsiConsole.Confirm("Are you sure you want to delete your account?");
            if (confirmation)
            {
                //Simulate account deletion
                AnsiConsole.MarkupLine("[bold green]✅ Account Deleted Successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold yellow]⚠️ Account Deletion Cancelled![/]");
            }
        }
        //Method to logout user
        public static void LogoutUser()
        {
            Console.Clear();
            var header = new FigletText("Logout")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            //confirmation before logout
            if (AnsiConsole.Confirm("Are you sure you want to logout?"))
            {
                //back to main menu
                AnsiConsole.MarkupLine("[bold green]✅ Logged Out Successfully![/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[bold yellow]⚠️ Logout Cancelled![/]");
            }
        }
        //Method to reset user password
        public static void ResetUserPassword(User user)
        {
            AnsiConsole.MarkupLine("\n[bold green]🔄 Reset Password[/]");
            var email = AnsiConsole.Ask<string>("Enter your Email:");
            if (user.Email != email)
            {
                AnsiConsole.MarkupLine("[bold red]❌ Invalid Email![/]");
                return;
            }
            user.PasswordHash = User.HashPassword(AnsiConsole.Ask<string>("Enter New Password:"));
            AnsiConsole.MarkupLine("[bold green]✅ Password Reset Successfully![/]");
        }
        //Method to display user settings
        public static void DisplayUserSettings(User user)
        {
            AnsiConsole.MarkupLine("\n[bold green]⚙️ User Settings[/]");
            AnsiConsole.MarkupLine($"[bold]Two-Factor Authentication:[/] {(user.IsTwoFactorEnabled ? "Enabled" : "Disabled")}");
            AnsiConsole.MarkupLine($"[bold]Email Verified:[/] {(user.IsEmailVerified ? "Yes" : "No")}");
            AnsiConsole.MarkupLine($"[bold]Phone Verified:[/] {(user.IsPhoneVerified ? "Yes" : "No")}");
        }
        //Method to toggle two-factor authentication
        public static void ToggleTwoFactorAuthentication(User user)
        {
            Console.Clear();
            var header = new FigletText("Toggle 2FA")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                if (user.IsTwoFactorEnabled)
                {
                    user.DisableTwoFactorAuthentication();
                    AnsiConsole.MarkupLine("[bold green]✅ Two-Factor Authentication Disabled![/]");
                }
                else
                {
                    user.EnableTwoFactorAuthentication();
                    AnsiConsole.MarkupLine("[bold green]✅ Two-Factor Authentication Enabled![/]");
                }
                Console.ReadLine();
                MainMenu(user);
                return;
            }
        }
        //Method to verify user email
        public static void VerifyUserEmail(User user)
        {
            Console.Clear();
            var header = new FigletText("Verify Email")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                user.VerifyEmail();
                AnsiConsole.MarkupLine("[bold green]✅ Email Verified Successfully![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
        }
        //Method to verify user phone number
        public static void VerifyUserPhone(User user)
        {
            Console.Clear();
            var header = new FigletText("Verify Phone")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                user.VerifyPhone();
                AnsiConsole.MarkupLine("[bold green]✅ Phone Number Verified Successfully![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
        }
        //Method to display user search history
        public static void DisplayUserSearchHistory(User user)
        {
            Console.Clear();
            var header = new FigletText("Search History")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                AnsiConsole.MarkupLine("\n[bold green]🕵️ User Search History[/]");
                if (user.SearchHistory.Count == 0)
                {
                    AnsiConsole.MarkupLine("[bold yellow]⚠️ No search history found![/]");
                }
                else
                {
                    for (int i = 0; i < user.SearchHistory.Count; i++)
                    {
                        AnsiConsole.MarkupLine($"[bold]{i + 1}.[/] {user.SearchHistory[i]}");
                    }
                }
                Console.ReadLine();
                MainMenu(user);
                return;
            }
        }
        //Method to clear user search history
        public static void ClearUserSearchHistory(User user)
        {
            Console.Clear();
            var header = new FigletText("Clear Search History")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);
            if (user == null)
            {
                AnsiConsole.MarkupLine("[bold red]❌ No user is currently logged in![/]");
                Console.ReadLine();
                MainMenu(user);
                return;
            }
            else
            {
                //connfirmation before clearing
                if (AnsiConsole.Confirm("Are you sure you want to clear your search history?"))
                {
                    user.ClearSearchHistory();
                    AnsiConsole.MarkupLine("[bold green]✅ Search History Cleared Successfully![/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[bold yellow]⚠️ Clear Search History Cancelled![/]");
                }
            }
        }
        //Method to display user two-factor authentication status
        public static void DisplayTwoFactorStatus(User user)
        {
            AnsiConsole.MarkupLine($"\n[bold green]🔐 Two-Factor Authentication is currently {(user.IsTwoFactorEnabled ? "Enabled" : "Disabled")}.[/]");
        }
        //Method to display user email verification status
        public static void DisplayEmailVerificationStatus(User user)
        {
            AnsiConsole.MarkupLine($"\n[bold green]📧 Email is currently {(user.IsEmailVerified ? "Verified" : "Not Verified")}.[/]");
        }
        //Method to display user phone verification status
        public static void DisplayPhoneVerificationStatus(User user)
        {
            AnsiConsole.MarkupLine($"\n[bold green]📱 Phone Number is currently {(user.IsPhoneVerified ? "Verified" : "Not Verified")}.[/]");
        }
        //Method to display user account summary
        public static void DisplayAccountSummary(User user)
        {
            AnsiConsole.MarkupLine("\n[bold green]📊 User Account Summary[/]");
            AnsiConsole.MarkupLine($"[bold]Username:[/] {user.Username}");
            AnsiConsole.MarkupLine($"[bold]Email Verified:[/] {(user.IsEmailVerified ? "Yes" : "No")}");
            AnsiConsole.MarkupLine($"[bold]Phone Verified:[/] {(user.IsPhoneVerified ? "Yes" : "No")}");
            AnsiConsole.MarkupLine($"[bold]Two-Factor Authentication:[/] {(user.IsTwoFactorEnabled ? "Enabled" : "Disabled")}");
            AnsiConsole.MarkupLine($"[bold]Search History Count:[/] {user.SearchHistory.Count}");
        }
        //Method to display user security settings
        public static void DisplayUserSecuritySettings(User user)
        {
            AnsiConsole.MarkupLine("\n[bold green]🔒 User Security Settings[/]");
            DisplayTwoFactorStatus(user);
            DisplayEmailVerificationStatus(user);
            DisplayPhoneVerificationStatus(user);
        }
        //Method to display user activity log
        public static void DisplayUserActivityLog(User user)
        {
            AnsiConsole.MarkupLine("\n[bold green]📝 User Activity Log[/]");
            AnsiConsole.MarkupLine("[bold yellow]⚠️ No activity log found![/]");
        }
        //Method to clear user activity log
        public static void ClearUserActivityLog(User user)
        {
            AnsiConsole.MarkupLine("[bold green]✅ Activity Log Cleared Successfully![/]");
        }
        //Main start method to demonstrate UIManager functionalities
        public static void Start(User user)
        {
            DisplayAccountSummary(user);
            DisplayUserSecuritySettings(user);
            DisplayUserActivityLog(user);
        }
        //MainMenu method to demonstrate UIManager functionalities with menu options selection 
        public static void MainMenu(User user)
        {
            Console.Clear();
            var header = new FigletText("AutoCompare")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);

            bool exit = false;
            while (!exit)
            {
                var option = DisplayMenu("Main Menu", new List<string>
                {
                    "🔍 Car Search",
                    "👤 View Profile",
                    "✏️ Update Profile",
                    "🔒 Change Password",
                    "🔐 Toggle Two-Factor Authentication",
                    "📧 Verify Email",
                    "📱 Verify Phone Number",
                    "🕵️ View Search History",
                    "🧹 Clear Search History",
                    "🚪 Logout",
                    "❌ Back To The Main Menu"
                });

                switch (option)
                {
                    case "🔍 Car Search":
                        //Call CarSearchMenu method from CarSearch class
                        var carSearch = new CarSearch();
                        carSearch.CarSearchMenu();
                        break;
                    case "👤 View Profile":
                        DisplayUserProfile(user);
                        break;
                    case "✏️ Update Profile":
                        UpdateUserProfile(user);
                        break;
                    case "🔒 Change Password":
                        ChangeUserPassword(user);
                        break;
                    case "🔐 Toggle Two-Factor Authentication":
                        ToggleTwoFactorAuthentication(user);
                        break;
                    case "📧 Verify Email":
                        VerifyUserEmail(user);
                        break;
                    case "📱 Verify Phone Number":
                        VerifyUserPhone(user);
                        break;
                    case "🕵️ View Search History":
                        DisplayUserSearchHistory(user);
                        break;
                    case "🧹 Clear Search History":
                        ClearUserSearchHistory(user);
                        break;
                    case "🚪 Logout":
                        LogoutUser();
                        exit = true;
                        break;
                    case "❌ Back To The Main Menu":
                        exit = true;
                        break;
                }
            }
        }
        //Register and Login Menu
        public static void StartMenu()
        {
            DisplayWelcomeMessage();
            Console.Clear();

            // Display header
            var header = new FigletText("Register / Login")
                .Centered()
                .Color(Color.Green);
            AnsiConsole.Write(header);

            var user = new User("", "", "", "");
            bool exitRequested = false;

            while (!exitRequested)
            {
                var selection = DisplayMenu(
                    "[bold yellow]Welcome! Please select an option:[/]",
                    new List<string>
                    {
                        "📝 Register",
                        "🔐 Login",
                        "❌ Exit"
                    });

                switch (selection)
                {
                    case "📝 Register":
                        RegisterUser(user);
                        break;

                    case "🔐 Login":
                        if (LoginUser(user))
                        {
                            AnsiConsole.MarkupLine("[green]✅ Login successful! Redirecting to main menu...[/]");
                            MainMenu(user);
                        }
                        else
                        {
                            AnsiConsole.MarkupLine("[bold red]❌ Login failed! Please try again.[/]");
                        }
                        break;

                    case "❌ Exit":
                        AnsiConsole.MarkupLine("\n[bold yellow]👋 Thank you for using the system. Goodbye![/]");
                        exitRequested = true;
                        break;

                    default:
                        AnsiConsole.MarkupLine("[red]⚠️ Invalid selection. Please try again.[/]");
                        break;
                }

                if (!exitRequested)
                {
                    AnsiConsole.MarkupLine("\n[grey]Press any key to return to the menu...[/]");
                    DisplayGoodbyeMessage();
                    Console.ReadKey(true);
                    Console.Clear();
                    AnsiConsole.Write(header);
                }
            }
        }
    }
    // Add this class to provide the missing EmailService functionality
    public static class EmailService
    {
        public static async Task SendAsync(string toEmail, string subject, string body)
        {
            // Replace these with your SMTP server details
            var smtpHost = Environment.GetEnvironmentVariable("SMTP_HOST") ?? "smtp.example.com";
            var smtpPort = int.TryParse(Environment.GetEnvironmentVariable("SMTP_PORT"), out var port) ? port : 587;
            var smtpUser = Environment.GetEnvironmentVariable("SMTP_USER") ?? "your@email.com";
            var smtpPass = Environment.GetEnvironmentVariable("SMTP_PASS") ?? "password";

            using (var client = new SmtpClient(smtpHost, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            })
            using (var message = new MailMessage(smtpUser, toEmail, subject, body))
            {
                await client.SendMailAsync(message);
            }
        }
    }
    // Add this class to provide the missing SmsService functionality
    public static class SmsService
    {
        public static async Task SendAsync(string toPhone, string message)
        {
            var accountSid = Environment.GetEnvironmentVariable("TWILIO_ACCOUNT_SID")
                ?? throw new InvalidOperationException("TWILIO_ACCOUNT_SID not set.");
            var authToken = Environment.GetEnvironmentVariable("TWILIO_AUTH_TOKEN")
                ?? throw new InvalidOperationException("TWILIO_AUTH_TOKEN not set.");
            TwilioClient.Init(accountSid, authToken);

            var fromNumber = Environment.GetEnvironmentVariable("TWILIO_FROM_NUMBER")
                ?? throw new InvalidOperationException("TWILIO_FROM_NUMBER not set.");

            await MessageResource.CreateAsync(
                body: message,
                from: new Twilio.Types.PhoneNumber(fromNumber),
                to: new Twilio.Types.PhoneNumber(toPhone)
            );
        }
    }
}