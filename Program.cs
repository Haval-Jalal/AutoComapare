using DotNetEnv;
namespace AutoCompare
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Env.Load();
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var uiManager = new UIManager();
            await uiManager.Start();
            
        }
    }
}