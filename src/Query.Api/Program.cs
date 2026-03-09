using System.Data;
using FastEndpoints;
using FastEndpoints.Swagger;
using Npgsql;
using Query.Core.Compilers;
using Query.Core.Ingestion;
using Query.Core.LLM;
using Query.Core.Storage;

var builder = WebApplication.CreateBuilder(args);

// FastEndpoints
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument();

// Database
builder.Services.AddScoped<IDbConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();

// Schema ingestion
builder.Services.AddTransient<SchemaContextBuilder>();

// Compiler registry
var sqlDialect = builder.Configuration.GetValue<string>("SqlDialect") ?? "postgres";
builder.Services.AddSingleton(_ => CompilerRegistry.CreateDefault(sqlDialect));

// LLM provider
var llmSection = builder.Configuration.GetSection("LLM");
var llmConfig = new LLMProviderConfig(
    BaseUrl: llmSection["BaseUrl"] ?? "https://api.openai.com",
    AuthScheme: llmSection["AuthScheme"] ?? "Bearer",
    AuthToken: llmSection["AuthToken"] ?? "",
    Model: llmSection["Model"] ?? "gpt-4");

builder.Services.AddSingleton(llmConfig);
builder.Services.AddHttpClient<ILLMProvider, HttpLLMProvider>(client =>
{
    client.BaseAddress = new Uri(llmConfig.BaseUrl);
});

var app = builder.Build();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
