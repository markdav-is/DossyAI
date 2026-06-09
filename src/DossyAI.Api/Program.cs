using DossyAI.Core.Data;
using DossyAI.Core.Services;
using DossyAI.Mcp;
using DossyAI.Api.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---- Database ----
var connectionString = builder.Configuration["DossyAi:Database:ConnectionString"]
    ?? builder.Configuration["DossyAi:Database:ConnectionString"]
    ?? "Server=(localdb)\\mssqllocaldb;Database=DossyAiDb;Trusted_Connection=true;";

builder.Services.AddDbContext<DossyAiDbContext>(options =>
    options.UseSqlServer(connectionString));

// ---- Embedding Service ----
var embeddingProvider = builder.Configuration["DossyAi:Embedding:Provider"] ?? "ollama";
if (embeddingProvider.Equals("openrouter", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddHttpClient<IEmbeddingService, OpenRouterEmbeddingService>();
}
else
{
    builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>();
}

// ---- Metadata Extractor ----
builder.Services.AddHttpClient<MetadataExtractor>();

// ---- Memory Service ----
builder.Services.AddScoped<IMemoryService, MemoryService>();

// ---- MCP Server ----
builder.Services.AddDossyAiMcp();

// ---- Authentication ----
builder.Services.AddAuthentication("ApiKey")
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });
builder.Services.AddAuthorization();

// ---- CORS ----
var allowedOrigins = builder.Configuration.GetSection("DossyAi:Mcp:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        if (allowedOrigins.Length > 0)
            policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod();
        else
            policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();

// ---- Auto-migrate ----
var autoMigrate = builder.Configuration.GetValue<bool>("DossyAi:Database:AutoMigrate", true);
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<DossyAiDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// ---- Health endpoint ----
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow }));

// ---- MCP endpoint ----
app.MapMcp("/mcp");

app.Run();
