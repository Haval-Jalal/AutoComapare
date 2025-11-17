using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Spectre.Console;

namespace AutoCompare
{
    namespace AutoCompare
    {
        using Spectre.Console;
        using System.Collections.Generic;
        using System.Linq;

        namespace AutoCompareApp
        {
            public class UIManager
            {

                private readonly ISecondFactorSender _sender;
                private readonly ICodeGenerator _codeGenerator;
                private readonly DataStore<User> _userStore = new DataStore<User>();
                
                // Tillfällig lista med användare i minnet (dummy)
                private List<(string Username, string Password)> _users = new();
                private string? _loggedInUser;

               
                public void Start()
                {
                    while (true)
                    {
                        AnsiConsole.Clear();
                        var title = new FigletText("AutoCompare")
                            .Color(Color.Green)
                            .Centered();
                        AnsiConsole.Write(title);
                        AnsiConsole.WriteLine();

                        var menu = new SelectionPrompt<string>()
                            .Title("[yellow]Välj ett alternativ:[/]")
                            .AddChoices(GetMenuChoices());

                        var choice = AnsiConsole.Prompt(menu);

                        switch (choice)
                        {
                            case "📝 Registrera dig":
                                Register();
                                break;
                            case "🔐 Logga in":
                                Login();
                                break;
                            case "📜 Visa profilinfo":
                                ShowProfile();
                                break;
                            case "🚪 Logga ut":
                                Logout();
                                break;
                            case "❌ Avsluta":
                                return;
                        }
                    }
                }

                private IEnumerable<string> GetMenuChoices()
                {
                    if (_loggedInUser == null)
                    {
                        return new[] { "📝 Registrera dig", "🔐 Logga in", "❌ Avsluta" };
                    }
                    else
                    {
                        return new[] { "📜 Visa profilinfo", "🚪 Logga ut", "❌ Avsluta" };
                    }
                }

                //REGISTRERA
                private void Register()
                {
                    AnsiConsole.MarkupLine("[yellow]Registrering[/]");
                    var username = AnsiConsole.Ask<string>("Ange användarnamn:");

                    // Kontrollera om användarnamnet finns
                    if (_userStore.FindItem(u => u.Username == username) != null)
                    {
                        AnsiConsole.MarkupLine("[red]Användarnamnet är upptaget.[/]");
                        Pause();
                        return;
                    }

                    var password = ReadHiddenPassword("Ange lösenord:");

                    // Välj 2FA-metod
                    var method = AnsiConsole.Prompt(
                        new SelectionPrompt<TwoFactorMethod>()
                            .Title("Välj [green]2FA-metod[/]:")
                            .AddChoices(TwoFactorMethod.Email, TwoFactorMethod.SMS));

                    string contact;

                    if (method == TwoFactorMethod.Email)
                    {
                        contact = AnsiConsole.Ask<string>("Ange e-postadress:");
                    }
                    else // SMS
                    {
                        contact = AnsiConsole.Ask<string>("Ange telefonnummer (inkl. landskod, t.ex. +46701234567):");
                    }

                    var tempUser = new User();

                    if (!tempUser.Register(username, password, method, contact, _userStore))
                    {
                        AnsiConsole.MarkupLine("[red]Registrering misslyckades.[/]");
                        Pause();
                        return;
                    }

                    // Skicka 2FA-kod
                    tempUser.SendTwoFactorCode(_sender, _codeGenerator, TimeSpan.FromMinutes(5));

                    var code = AnsiConsole.Ask<string>("Ange verifieringskoden du fick:");

                    if (!tempUser.VerifyTwoFactorCode(code))
                    {
                        AnsiConsole.MarkupLine("[red]Felaktig eller utgången kod.[/]");
                        Pause();
                        return;
                    }

                    _userStore.AddItem(tempUser);
                    AnsiConsole.MarkupLine($"[green]Kontot {username} har verifierats och registrerats![/]");
                    Pause();
                }

                //LOGGA IN
                private void Login()
                {
                    AnsiConsole.MarkupLine("[yellow]Inloggning[/]");
                    var username = AnsiConsole.Ask<string>("Ange användarnamn:");
                    var password = ReadHiddenPassword("Ange lösenord:");

                    var match = _users.FirstOrDefault(u => u.Username == username && u.Password == password);
                    if (match == default)
                    {
                        AnsiConsole.MarkupLine("[red]Fel användarnamn eller lösenord.[/]");
                    }
                    else
                    {
                        _loggedInUser = username;
                        AnsiConsole.MarkupLine($"[green]Välkommen tillbaka, {username}![/]");
                    }
                    Pause();
                }

                private void ShowProfile()
                {
                    if (_loggedInUser == null)
                    {
                        AnsiConsole.MarkupLine("[red]Du är inte inloggad.[/]");
                        Pause();
                        return;
                    }

                    AnsiConsole.Write(new Rule($"[bold yellow]{_loggedInUser}s profil[/]").RuleStyle("grey"));
                    AnsiConsole.MarkupLine($"[green]Användarnamn:[/] {_loggedInUser}");
                    AnsiConsole.MarkupLine("[grey](Inga fler uppgifter ännu — detta är bara en testmall.)[/]");
                    Pause();
                }

                private void Logout()
                {
                    if (_loggedInUser != null)
                    {
                        AnsiConsole.MarkupLine($"[grey]{_loggedInUser} loggades ut.[/]");
                        _loggedInUser = null;
                    }
                    Pause();
                }

                // Hjälpmetod för dold lösenordsinmatning
                private string ReadHiddenPassword(string prompt)
                {
                    AnsiConsole.MarkupLine($"[grey]{prompt}[/]");
                    var password = string.Empty;
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
                            if (password.Length > 0)
                            {
                                password = password[..^1];
                                Console.Write("\b \b");
                            }
                        }
                        else
                        {
                            password += keyInfo.KeyChar;
                            Console.Write("*");
                        }
                    }

                    return password;
                }

                private void Pause()
                {
                    AnsiConsole.MarkupLine("\nTryck på valfri tangent för att fortsätta...");
                    Console.ReadKey(true);
                }




            }
        }

    }
}
