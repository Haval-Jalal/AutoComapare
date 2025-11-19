namespace AutoCompare
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var ui = new UIManager();
            ui.Start();
        }
    }
}