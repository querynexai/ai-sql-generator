using AiSqlGenerator.Api.Data;
using AiSqlGenerator.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI; // <-- Required

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// In Program.cs
var apiKey = builder.Configuration["Groq:ApiKey"]; // Store this in user-secrets
var modelId = "openai/gpt-oss-120b"; // A powerful and fast model
var endpoint = "https://api.groq.com/openai/v1"; // Groq's OpenAI-compatible endpoint

builder.Services.AddSingleton<IChatCompletionService>(sp =>
    new OpenAIChatCompletionService(
        modelId: modelId,
        endpoint: new Uri(endpoint),
        apiKey: apiKey
    ));

// Register HttpClient for QueryGeneratorService
builder.Services.AddHttpClient<QueryGeneratorService>();
builder.Services.AddScoped<SchemaProvider>();
builder.Services.AddScoped<SqlExecutionService>();
// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ---- Controllers + Swagger ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.MapControllers();

app.Run();