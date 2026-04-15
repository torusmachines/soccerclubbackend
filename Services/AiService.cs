using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FootballDashboardAPI.Services;

public interface IAiService
{
    Task<string> GeneratePlayerDevelopmentPlanAsync(string prompt);
}

public class OpenAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<OpenAiService> _logger;

    public OpenAiService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:OpenAI:ApiKey"] ?? configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey not configured");
        _model = configuration["AI:OpenAI:Model"] ?? configuration["OpenAI:Model"] ?? "gpt-4";
        _logger = logger;
    }

    public async Task<string> GeneratePlayerDevelopmentPlanAsync(string prompt)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = GetSystemPrompt() },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            max_tokens = 4000
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Headers = { { "Authorization", $"Bearer {_apiKey}" } },
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("OpenAI request failed: {StatusCode} {ReasonPhrase} - {ErrorText}", response.StatusCode, response.ReasonPhrase, errorText);
            throw new Exception($"OpenAI request failed: {response.StatusCode} {response.ReasonPhrase} - {errorText}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonDocument.Parse(responseContent);

        var content = responseJson.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(content))
        {
            throw new Exception("Empty response from AI service");
        }

        return content!;
    }

    private string GetSystemPrompt()
    {
        return @"You are an elite football performance coach, sports scientist, and professional training planner.

STRICT RULES:

* No unrealistic improvement claims
* Technical skills: 0.5–1 point improvement per 4–6 weeks
* Physical attributes: 8–12 weeks minimum for meaningful gains
* Age impacts progress speed (players >30 have slower development)
* Always include recovery and rest periods
* Detect injury risks from medical notes (keywords: knee, ankle, hamstring, groin, shoulder)
* Base recommendations on historical data and trends
* Provide realistic, evidence-based training plans

OUTPUT FORMAT (STRICT JSON):
{
  ""summary"": ""Brief overview of player's current status and development potential"",
  ""strengths"": [""List of key strengths identified from data""],
  ""weaknesses"": [""List of areas needing improvement""],
  ""trend_analysis"": ""Analysis of performance trends over time"",
  ""injury_risks"": [""Identified injury risks based on notes and history""],
  ""improvements_from_last_plan"": [""What has improved since last plan (if applicable)""],
  ""timeline_weeks"": {""4"": ""Short-term goals"", ""8"": ""Medium-term goals"", ""12"": ""Long-term goals""},
  ""skill_plan"": {""passing"": [""Week 1-2: Focus drill"", ""Week 3-4: Game application""], ""shooting"": [""Weekly progression""]},
  ""weekly_schedule"": {""monday"": [""Technical training"", ""Recovery""], ""tuesday"": [""Tactical work""]},
  ""performance_tracking"": [""Metrics to monitor progress""],
  ""recommendations"": [""Specific training recommendations""]
}";
    }
}

public class GroqAiService : IAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<GroqAiService> _logger;

    public GroqAiService(HttpClient httpClient, IConfiguration configuration, ILogger<GroqAiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:Groq:ApiKey"] ?? configuration["Groq:ApiKey"] ?? throw new ArgumentNullException("Groq:ApiKey not configured");
        _model = configuration["AI:Groq:Model"] ?? configuration["Groq:Model"] ?? "mixtral-8x7b-32768";
        _logger = logger;
    }

    public async Task<string> GeneratePlayerDevelopmentPlanAsync(string prompt)
    {
        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = GetSystemPrompt() },
                new { role = "user", content = prompt }
            },
            temperature = 0.2,
            max_tokens = 4000
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions")
        {
            Headers = { { "Authorization", $"Bearer {_apiKey}" } },
            Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            _logger.LogError("Groq request failed: {StatusCode} {ReasonPhrase} - {ErrorText}", response.StatusCode, response.ReasonPhrase, errorText);
            throw new Exception($"Groq request failed: {response.StatusCode} {response.ReasonPhrase} - {errorText}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JsonDocument.Parse(responseContent);

        var content = responseJson.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrEmpty(content))
        {
            throw new Exception("Empty response from AI service");
        }

        return content!;
    }

    private string GetSystemPrompt()
    {
        return @"You are an elite football performance coach, sports scientist, and professional training planner.

STRICT RULES:

* No unrealistic improvement claims
* Technical skills: 0.5–1 point improvement per 4–6 weeks
* Physical attributes: 8–12 weeks minimum for meaningful gains
* Age impacts progress speed (players >30 have slower development)
* Always include recovery and rest periods
* Detect injury risks from medical notes (keywords: knee, ankle, hamstring, groin, shoulder)
* Base recommendations on historical data and trends
* Provide realistic, evidence-based training plans

OUTPUT FORMAT (STRICT JSON):
{
  ""summary"": ""Brief overview of player's current status and development potential"",
  ""strengths"": [""List of key strengths identified from data""],
  ""weaknesses"": [""List of areas needing improvement""],
  ""trend_analysis"": ""Analysis of performance trends over time"",
  ""injury_risks"": [""Identified injury risks based on notes and history""],
  ""improvements_from_last_plan"": [""What has improved since last plan (if applicable)""],
  ""timeline_weeks"": {""4"": ""Short-term goals"", ""8"": ""Medium-term goals"", ""12"": ""Long-term goals""},
  ""skill_plan"": {""passing"": [""Week 1-2: Focus drill"", ""Week 3-4: Game application""], ""shooting"": [""Weekly progression""]},
  ""weekly_schedule"": {""monday"": [""Technical training"", ""Recovery""], ""tuesday"": [""Tactical work""]},
  ""performance_tracking"": [""Metrics to monitor progress""],
  ""recommendations"": [""Specific training recommendations""]
}";
    }
}