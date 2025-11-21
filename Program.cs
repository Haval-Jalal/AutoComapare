namespace AutoCompare
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var uiManager = new UIManager();
            await uiManager.Start();
        }
    }
}