# Local Development Setup

## Prerequisites

- .NET 10 SDK
- SQL Server (LocalDB, Docker, or Azure SQL)
- Ollama (for local embeddings) or OpenRouter API key

## Quick Start

### 1. Clone and restore

```bash
git clone https://github.com/your-org/DossyAI.git
cd DossyAI
dotnet restore
```

### 2. Configure settings

Copy and edit `appsettings.Development.json` in `src/DossyAI.Api/`:

```json
{
  "DossyAi": {
    "Database": {
      "ConnectionString": "Server=(localdb)\\mssqllocaldb;Database=DossyAiDb;Trusted_Connection=true;"
    },
    "Embedding": {
      "Provider": "ollama",
      "OllamaBaseUrl": "http://localhost:11434",
      "Model": "nomic-embed-text"
    }
  }
}
```

### 3. Run migrations (optional — auto-migrates on startup)

```bash
cd src/DossyAI.Api
dotnet ef database update
```

### 4. Run the API

```bash
dotnet run --project src/DossyAI.Api
```

The MCP server will be available at `http://localhost:5000/mcp`.

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `DossyAi__Database__ConnectionString` | SQL Server connection string | LocalDB |
| `DossyAi__Mcp__ApiKey` | API key for authentication | (empty = no auth) |
| `DossyAi__Embedding__Provider` | `ollama` or `openrouter` | `ollama` |
| `DossyAi__Embedding__OllamaBaseUrl` | Ollama server URL | `http://localhost:11434` |
| `DossyAi__Embedding__Model` | Embedding model name | `nomic-embed-text` |
| `DossyAi__LLM__Provider` | `ollama` or `openrouter` | `ollama` |
| `DossyAi__LLM__Model` | LLM model for metadata extraction | `mistral` |

## Running Tests

```bash
dotnet test
```
