using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AiSqlGenerator.Api.Services;

public class QueryGeneratorService
{
    private readonly HttpClient _http;
    private readonly SchemaProvider _schemaProvider;
    private readonly string _apiKey;
    private readonly string _model;

    public QueryGeneratorService(
        HttpClient http,
        SchemaProvider schemaProvider,
        IConfiguration config)
    {
        _http = http;
        _schemaProvider = schemaProvider;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not set");
        _model = config["Groq:Model"] ?? "llama-3.3-70b-versatile";

        _http.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<string> GenerateSqlAsync(string naturalLanguageQuery)
    {
        var schema = await _schemaProvider.GetSchemaDescriptionAsync();

        var systemPrompt = $"""
            You are an expert PostgreSQL developer.
            Convert the user's natural language question into a SINGLE, valid PostgreSQL SELECT query.

            DATABASE SCHEMA:
            {schema}

            RULES:
            1. Return ONLY the SQL query. No explanations, no markdown, no code fences.
            2. Use ILIKE for case-insensitive text matching.
            3. Use proper JOIN syntax when multiple tables are needed.
            4. Add LIMIT 100 if the query might return many rows.
            5. Never use INSERT, UPDATE, DELETE, DROP, ALTER, or any data-modifying statement.
            6. Only generate SELECT statements.
            """;

        var payload = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = naturalLanguageQuery }
            },
            temperature = 0.2
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Groq API error ({response.StatusCode}): {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        var sql = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;

        return sql.Replace("```sql", "").Replace("```", "").Trim();
    }
}