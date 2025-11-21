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

    public class AIService
    {
        private readonly AiHelper _aiHelper;

        public AIService()
        {
            _aiHelper = new AiHelper(); // Uses .env OPENAI_API_KEY internally
        }

        // Main method to ask AI about a car model
        public async Task<AiResult> AskCarModelAsync(string userQuestion, IEnumerable<Car>? localContext = null)
        {
            if (string.IsNullOrWhiteSpace(userQuestion))
                throw new ArgumentException("Question must not be empty", nameof(userQuestion));

            string contextText = BuildLocalContext(localContext);

            // System prompt instructs AI to be factual and use local context
            var systemPrompt = new StringBuilder();
            systemPrompt.AppendLine("You are a factual automotive expert.");
            systemPrompt.AppendLine("Always base your answer on the provided local context first.");
            systemPrompt.AppendLine("If needed, supplement with reliable external info.");
            systemPrompt.AppendLine("Respond only in JSON with properties: answer (detailed), summary (1-2 sentences), sources (list of URLs).");
            systemPrompt.AppendLine("Do NOT include text outside JSON.");

            var userPrompt = new StringBuilder();
            if (!string.IsNullOrEmpty(contextText))
            {
                userPrompt.AppendLine("Local car data:");
                userPrompt.AppendLine(contextText);
                userPrompt.AppendLine();
            }
            userPrompt.AppendLine("Question:");
            userPrompt.AppendLine(userQuestion);
            userPrompt.AppendLine("Return JSON only.");

            try
            {
                string raw = await _aiHelper.ChatAsync(systemPrompt.ToString(), userPrompt.ToString());
                return ParseAiResult(raw);
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                return new AiResult
                {
                    Answer = "Sorry, an error occurred while fetching AI data.",
                    Summary = ex.Message,
                    Sources = new List<string>()
                };
            }
        }

        // Build a string summary of local car data to provide context to AI
        private string BuildLocalContext(IEnumerable<Car>? cars)
        {
            if (cars == null || !cars.Any())
                return string.Empty;

            var sb = new StringBuilder();
            foreach (var car in cars)
            {
                sb.AppendLine($"Brand: {car.Brand}, Model: {car.Model}, Year: {car.Year}, Mileage: {car.Mileage}, Owners: {car.Owners}, Known Issues: {string.Join(", ", car.KnownIssues)}");
            }
            return sb.ToString();
        }

        // Parse the JSON returned by AI safely
        private AiResult ParseAiResult(string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                var result = new AiResult
                {
                    Answer = root.GetProperty("answer").GetString() ?? string.Empty,
                    Summary = root.GetProperty("summary").GetString() ?? string.Empty,
                    Sources = new List<string>()
                };

                if (root.TryGetProperty("sources", out var sourcesElement) && sourcesElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in sourcesElement.EnumerateArray())
                        result.Sources.Add(item.GetString() ?? string.Empty);
                }

                return result;
            }
            catch
            {
                // If JSON parse fails, return raw text as answer
                return new AiResult
                {
                    Answer = rawJson,
                    Summary = string.Empty,
                    Sources = new List<string>()
                };
            }
        }
    }
}