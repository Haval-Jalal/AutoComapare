using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AutoCompare
{
    // Simple DTO for AI results
    public class AiResult
    {
        public string Answer { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public List<string> Sources { get; set; } = new();
    }

    public class AIService : IDisposable
    {
        private readonly AiHelper _aiHelper;
        private bool _disposed;

        public AIService()
        {
            _aiHelper = new AiHelper();
        }

        // Ask the model about a car model. Uses chat-style messages for better multi-turn support.
        // localContext: optional list of Car objects (small summary will be added as a system message).
        public async Task<AiResult> AskCarModelAsync(string userQuestion, IEnumerable<Car>? localContext = null)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                throw new ArgumentException("Question must not be empty", nameof(userQuestion));

            // Build system prompt - instruct model to be factual, output optionally in markdown
            var systemPrompt = new StringBuilder();
            systemPrompt.AppendLine("You are an expert automotive assistant. Answer clearly and factually.");
            systemPrompt.AppendLine("If the user asks about a specific car model, provide structured information (Overview, Specifications, Performance, Safety, Maintenance, Common Issues).");
            systemPrompt.AppendLine("Use markdown headings (###) for sections and use bullet lists or key:value for specifications.");
            systemPrompt.AppendLine("If you cite sources, return them in a 'Sources' section at the end.");
            systemPrompt.AppendLine("Be concise but thorough. Respond in English.");

            // Build messages list (roles: system, user)
            var messages = new List<(string role, string content)>
            {
                ("system", systemPrompt.ToString())
            };

            // If we have localContext, add a system message that lists local known cars (so model prefers local info)
            if (localContext != null && localContext.Any())
            {
                var ctxSb = new StringBuilder();
                ctxSb.AppendLine("Local car data (for context):");
                foreach (var c in localContext)
                {
                    ctxSb.AppendLine($"- Brand: {c.Brand}, Model: {c.Model}, Year: {c.Year}, Mileage: {c.Mileage}, Owners: {c.Owners}, KnownIssues: {string.Join(", ", c.KnownIssues ?? new List<string>())}");
                }
                messages.Add(("system", ctxSb.ToString()));
            }

            // Add user's question as a user message
            messages.Add(("user", userQuestion));

            try
            {
                // Call AiHelper which sends a messages array
                string raw = await _aiHelper.ChatMessagesAsync(messages, model: "gpt-4o-mini", maxTokens: 1000, temperature: 0.0);

                // Parse returned text to AiResult (attempt JSON parse, else use markdown parsing later)
                // If the model returns JSON, try to parse; otherwise put raw into Answer and compute a one-line summary.
                var parsed = ParseAiResult(raw);
                if (string.IsNullOrWhiteSpace(parsed.Summary))
                {
                    // If no summary provided by AI result, create a small extracted summary (first 1-2 sentences)
                    parsed.Summary = CreateShortSummary(parsed.Answer);
                }

                return parsed;
            }
            catch (Exception ex)
            {

                Logger.Log($"AskCarModelAsync error:", ex);
                // Return a graceful error AiResult for the caller to display
                return new AiResult
                {
                    Answer = $"Sorry, an error occurred while fetching AI data: {ex.Message}",
                    Summary = "Error fetching AI data",
                    Sources = new List<string>()
                };
            }
        }

        // If AI returned JSON (rare), we try to parse it into AiResult.
        // Otherwise we set Answer=raw and Summary empty.
        private AiResult ParseAiResult(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return new AiResult { Answer = string.Empty, Summary = string.Empty, Sources = new List<string>() };

            // Try JSON parse first (assistant might return JSON)
            try
            {
                using var doc = JsonDocument.Parse(raw.Trim());
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    var result = new AiResult();
                    if (root.TryGetProperty("answer", out var ans)) result.Answer = ans.GetString() ?? string.Empty;
                    if (root.TryGetProperty("summary", out var sum)) result.Summary = sum.GetString() ?? string.Empty;
                    if (root.TryGetProperty("sources", out var src) && src.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var s in src.EnumerateArray())
                            result.Sources.Add(s.GetString() ?? string.Empty);
                    }
                    // If answer empty but there are other fields, fallback to full raw
                    if (string.IsNullOrWhiteSpace(result.Answer)) result.Answer = raw;
                    return result;
                }
            }
            catch
            {
                // Not JSON — continue below
            }

            // Not JSON — return raw answer. Try to extract a sources list from "### Sources" or URLs
            var aiResult = new AiResult
            {
                Answer = raw,
                Summary = string.Empty,
                Sources = ExtractUrlsFromText(raw)
            };

            return aiResult;
        }

        private string CreateShortSummary(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Very simple heuristic: take first two sentences (split by .!?)
            var sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s))
                                .ToList();
            if (!sentences.Any()) return text.Length <= 200 ? text : text.Substring(0, 197) + "...";
            if (sentences.Count == 1) return sentences[0].Length <= 200 ? sentences[0] : sentences[0].Substring(0, 197) + "...";
            var summary = $"{sentences[0]}. {sentences[1]}.";
            return summary.Length <= 200 ? summary : (summary.Substring(0, 197) + "...");
        }

        private List<string> ExtractUrlsFromText(string text)
        {
            var urls = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return urls;

            var urlRegex = new System.Text.RegularExpressions.Regex(@"https?://[^\s\)\]]+", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (System.Text.RegularExpressions.Match m in urlRegex.Matches(text))
                urls.Add(m.Value.Trim());

            return urls;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _aiHelper.Dispose();
                _disposed = true;
            }
        }
    }
}