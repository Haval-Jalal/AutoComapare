using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoCompare
{
    // Low-level chat client that sends role-labelled messages to OpenAI chat completions.
    public class AiHelper : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private const string ChatUrl = "https://api.openai.com/v1/chat/completions";
        private bool _disposed;

        public AiHelper()
        {
            _apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY is missing. Add it to .env or environment variables.");

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Send a chat request using role-labelled messages.
        // messages: list of (role, content) pairs. role should be "system", "user", or "assistant".
        // model: model name to use (defaults to gpt-4o-mini).
        // maxTokens: token limit for the response.
        public async Task<string> ChatMessagesAsync(IEnumerable<(string role, string content)> messages,
                                                    string model = "gpt-4o-mini",
                                                    int maxTokens = 600,
                                                    double temperature = 0.0)
        {
            if (messages == null) throw new ArgumentNullException(nameof(messages));

            // Build messages array for JSON payload
            var payloadMessages = new List<object>();
            foreach (var m in messages)
            {
                if (string.IsNullOrWhiteSpace(m.role) || string.IsNullOrWhiteSpace(m.content)) continue;
                payloadMessages.Add(new { role = m.role, content = m.content });
            }

            var payload = new
            {
                model = model,
                messages = payloadMessages,
                max_tokens = maxTokens,
                temperature = temperature
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var resp = await _http.PostAsync(ChatUrl, content);
            var respBody = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                // Return a helpful exception with API body for debugging/logging
                throw new Exception($"OpenAI API error {(int)resp.StatusCode}: {respBody}");
            }

            try
            {
                using var doc = JsonDocument.Parse(respBody);
                // Typical shape: { choices: [ { message: { role: "...", content: "..." } } ], ... }
                var root = doc.RootElement;
                if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                {
                    var first = choices[0];
                    if (first.TryGetProperty("message", out var messageElem) &&
                        messageElem.TryGetProperty("content", out var contentElem))
                    {
                        return contentElem.GetString() ?? string.Empty;
                    }

                    // Some models return text in "text" (legacy) - handle defensively
                    if (first.TryGetProperty("text", out var textElem))
                        return textElem.GetString() ?? string.Empty;
                }

                // Fallback: return the entire body if parsing failed to find message content
                return respBody;
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
}