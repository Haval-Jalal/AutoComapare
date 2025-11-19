namespace AutoCompare
{
    internal class Program
    {
        static void Main(string[] args)
        {
            //Ställ in konsolens utdata för att stödja UTF-8-tecken
            Console.OutputEncoding = System.Text.Encoding.UTF8;


            var ui = new UIManager();
            ui.Start();
        }
    }
}
