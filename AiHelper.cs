using System.Runtime.CompilerServices;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// AiHelper: low-level OpenAI chat client wrapper.
// Responsibilities:
// - Read OPENAI_API_KEY from environment
// - Send chat request with a provided system+user prompt
// - Return response string (raw)
// - Throw meaningful exceptions for callers to handle
public class AiHelper : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private const string ChatUrl = "https://api.openai.com/v1/chat/completions";
    private bool _disposed;

    public AiHelper()
    {
        // Load API key from environment (DotEnv should already be loaded in Program)
        _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? throw new InvalidOperationException("OPENAI_API_KEY is missing. Add it to .env or environment variables.");

        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30) // sensible default timeout
        };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // Sends a chat request and returns the model's content as string.
    // systemPrompt: instructions for the assistant.
    // userPrompt: the user's question + any context.
    // model: optional model name (defaults to gpt-4o-mini).
    public async Task<string> ChatAsync(string systemPrompt, string userPrompt, string model = "gpt-4o-mini", int maxTokens = 600)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt)) throw new ArgumentException("systemPrompt is required", nameof(systemPrompt));
        if (string.IsNullOrWhiteSpace(userPrompt)) throw new ArgumentException("userPrompt is required", nameof(userPrompt));

        var messages = new object[]
        {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userPrompt }
        };

        var payload = new
        {
            model = model,
            messages = messages,
            max_tokens = maxTokens,
            temperature = 0.0 // prefer factual answers
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await _http.PostAsync(ChatUrl, content);
        var respBody = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            // Include response body in exception to ease debugging
            throw new Exception($"OpenAI API error: {(int)resp.StatusCode}. Body: {respBody}");
        }

        try
        {
            using var doc = JsonDocument.Parse(respBody);
            var message = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            return message ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to parse OpenAI response JSON.", ex);
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _http.Dispose();
            _disposed = true;
        }
    }
}