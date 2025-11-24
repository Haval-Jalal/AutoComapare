using DotNetEnv;
namespace AutoCompare
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Env.Load();
            var uiManager = new UIManager();
            await uiManager.Start();
            
        }
    }
}