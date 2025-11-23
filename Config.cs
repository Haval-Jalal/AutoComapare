using AutoCompare;//ny
using DotNetEnv;

public static class Config
{
    private static bool _loaded = false;

    public static void Load()
    {
        if (_loaded)
            return;

        try
        {
            Env.Load();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Failed to load .env file: {ex.Message}");
            Console.ResetColor();
            Logger.Log($"Config Load error: {ex.Message}");
        }

        _loaded = true;
    }

    public static string Get(string key)
    {
        Load();
        return Environment.GetEnvironmentVariable(key) ?? "";
    }

    public static void Require(params string[] keys)
    {
        Load();

        try
        {
            foreach (var k in keys)
            {
                if (string.IsNullOrWhiteSpace(Get(k)))
                    throw new Exception($"Missing required configuration: {k}");
            }
        }
        catch (Exception ex)
        {
            Logger.Log($"Config Require error: {ex.Message}");
            throw; // kasta vidare om du vill att felet ska stoppa exekvering
        }
    }
}