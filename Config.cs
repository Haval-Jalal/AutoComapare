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
        foreach (var k in keys)
        {
            if (string.IsNullOrWhiteSpace(Get(k)))
                throw new Exception($"Missing required configuration: {k}");
        }
    }
}