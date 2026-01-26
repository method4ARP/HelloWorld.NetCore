using System.Text;
using System.Text.Json;
using HelloWorld.NetCore.Models;
using Microsoft.Extensions.Caching.Memory;

namespace HelloWorld.NetCore.Services
{
    public class BibleReadingAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<BibleReadingAIService> _logger;
        private readonly IMemoryCache _cache;
        private readonly string _provider;

        public BibleReadingAIService(HttpClient httpClient, IConfiguration configuration, ILogger<BibleReadingAIService> logger, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _provider = configuration["AI:Provider"] ?? "OpenAI";
            _apiKey = configuration[$"{_provider}:ApiKey"] ?? "";
            _logger = logger;
            _cache = cache;
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogInformation("AI Provider configured: {Provider}", _provider);
            }
        }

        public async Task<List<DailyReading>> GenerateReadingsAsync(string ageGroup, string gender)
        {
            // Check cache first
            var cacheKey = $"BibleReadings_{ageGroup}_{gender}";
            if (_cache.TryGetValue(cacheKey, out List<DailyReading>? cachedReadings))
            {
                _logger.LogInformation("Returning cached readings for {AgeGroup} - {Gender}", ageGroup, gender);
                return cachedReadings!;
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                _logger.LogWarning("OpenAI API key not configured. Using fallback readings.");
                return GetFallbackReadings(ageGroup, gender);
            }

            try
            {
                var prompt = BuildPrompt(ageGroup, gender);
                var response = await CallOpenAIAsync(prompt);
                var readings = ParseAIResponse(response);
                
                if (readings.Any())
                {
                    // Cache for 24 hours
                    _cache.Set(cacheKey, readings, TimeSpan.FromHours(24));
                    return readings;
                }
                
                return GetFallbackReadings(ageGroup, gender);
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                _logger.LogWarning("OpenAI API rate limit reached. Using fallback readings. Consider adding caching or upgrading your API plan.");
                return GetFallbackReadings(ageGroup, gender);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling OpenAI API. Using fallback readings.");
                return GetFallbackReadings(ageGroup, gender);
            }
        }

        private string BuildPrompt(string ageGroup, string gender)
        {
            var ageDescription = ageGroup switch
            {
                "Child" => "children aged 5-12",
                "Teen" => "teenagers aged 13-17",
                "YoungAdult" => "young adults aged 18-30",
                "Adult" => "adults aged 31-60",
                "Senior" => "seniors aged 60+",
                _ => "adults"
            };

            var genderText = gender == "All" ? "" : $" Focus on themes relevant to {gender.ToLower()} readers.";

            return $@"Create a 7-day Bible reading plan for {ageDescription}.{genderText}

For each day (Monday through Sunday), provide:
1. A specific Bible passage reference (book, chapter, and verses)
2. A brief theme or note about the passage (one sentence, under 60 characters)

Format each day exactly as:
Day: [Day of week]
Reading: [Bible reference]
Notes: [Brief theme]

Make the readings age-appropriate, meaningful, and encourage spiritual growth.";
        }

        private async Task<string> CallOpenAIAsync(string prompt)
        {
            var endpoint = _provider switch
            {
                "Groq" => "https://api.groq.com/openai/v1/chat/completions",
                "xAI" => "https://api.x.ai/v1/chat/completions",
                "Gemini" => "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent",
                _ => "https://api.openai.com/v1/chat/completions"
            };

            if (_provider == "Gemini")
            {
                return await CallGeminiAsync(prompt);
            }

            var model = _provider switch
            {
                "Groq" => "llama-3.1-8b-instant", // Fast and free (updated model)
                "xAI" => "grok-beta", // X.AI's Grok model
                _ => "gpt-3.5-turbo"
            };

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are a biblical scholar and pastor who creates meaningful Bible reading plans." },
                    new { role = "user", content = prompt }
                },
                temperature = 0.7,
                max_tokens = 800
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            _logger.LogInformation("Calling {Provider} API at {Endpoint} with model {Model}", _provider, endpoint, model);

            var response = await _httpClient.PostAsync(endpoint, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("API call failed with status {Status}: {Error}", response.StatusCode, errorContent);
            }
            
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);
            
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "";
        }

        private async Task<string> CallGeminiAsync(string prompt)
        {
            var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key={_apiKey}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = "You are a biblical scholar and pastor who creates meaningful Bible reading plans.\n\n" + prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.7,
                    maxOutputTokens = 800
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(responseJson);
            
            return doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString() ?? "";
        }

        private List<DailyReading> ParseAIResponse(string response)
        {
            var readings = new List<DailyReading>();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            string? currentDay = null;
            string? currentReading = null;
            string? currentNotes = null;

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                
                if (trimmed.StartsWith("Day:", StringComparison.OrdinalIgnoreCase))
                {
                    currentDay = trimmed.Substring(4).Trim();
                }
                else if (trimmed.StartsWith("Reading:", StringComparison.OrdinalIgnoreCase))
                {
                    currentReading = trimmed.Substring(8).Trim();
                }
                else if (trimmed.StartsWith("Notes:", StringComparison.OrdinalIgnoreCase))
                {
                    currentNotes = trimmed.Substring(6).Trim();
                    
                    if (!string.IsNullOrEmpty(currentDay) && !string.IsNullOrEmpty(currentReading))
                    {
                        readings.Add(new DailyReading
                        {
                            Day = currentDay,
                            Reading = currentReading,
                            Notes = currentNotes
                        });
                    }
                    
                    currentDay = null;
                    currentReading = null;
                    currentNotes = null;
                }
            }

            return readings;
        }

        private List<DailyReading> GetFallbackReadings(string ageGroup, string gender)
        {
            // Simple fallback if API fails
            return new List<DailyReading>
            {
                new DailyReading { Day = "Monday", Reading = "Psalm 23", Notes = "The Lord is my shepherd" },
                new DailyReading { Day = "Tuesday", Reading = "John 3:16-21", Notes = "God's love for the world" },
                new DailyReading { Day = "Wednesday", Reading = "Philippians 4:4-13", Notes = "Rejoice in the Lord always" },
                new DailyReading { Day = "Thursday", Reading = "Matthew 5:1-12", Notes = "The Beatitudes" },
                new DailyReading { Day = "Friday", Reading = "Romans 8:28-39", Notes = "More than conquerors" },
                new DailyReading { Day = "Saturday", Reading = "Ephesians 6:10-18", Notes = "The armor of God" },
                new DailyReading { Day = "Sunday", Reading = "1 Corinthians 13", Notes = "The greatest is love" }
            };
        }
    }
}
