# DossyAI — Persistent Memory System for AI Agents

DossyAI is an MCP (Model Context Protocol) server that gives AI agents persistent, searchable memory backed by SQL Server with semantic search via embeddings.

## Features

- **7 MCP tools**: store, find, retrieve, list, update, delete, stats
- **Semantic search** via cosine similarity on vector embeddings
- **Pluggable embedding providers**: Ollama (local) or OpenRouter
- **Automatic metadata extraction** via LLM
- **API key authentication**
- **SQL Server** storage with EF Core

## Quick Start

### Docker

```bash
git clone https://github.com/your-org/DossyAI.git
cd DossyAI
docker compose -f docker/docker-compose.yml up -d
```

Server starts at `http://localhost:5000/mcp`.

### Local Development

```bash
dotnet restore
dotnet run --project src/DossyAI.Api
```

See [docs/SETUP.md](docs/SETUP.md) for full setup instructions.

## MCP Client Configuration

### Claude Desktop

```json
{
  "mcpServers": {
    "dossyai": {
      "url": "http://localhost:5000/mcp",
      "headers": {
        "x-dossyai-key": "dossyai-key-dev"
      }
    }
  }
}
```

### VS Code (Continue)

```json
{
  "mcpServers": [
    {
      "name": "dossyai",
      "transport": {
        "type": "http",
        "url": "http://localhost:5000/mcp",
        "headers": { "x-dossyai-key": "dossyai-key-dev" }
      }
    }
  ]
}
```

See [docs/MCP_CONFIGURATION.md](docs/MCP_CONFIGURATION.md) for more clients.

## Documentation

- [Architecture](docs/ARCHITECTURE.md)
- [API Reference](docs/API_REFERENCE.md)
- [Setup Guide](docs/SETUP.md)
- [Deployment](docs/DEPLOYMENT.md)
- [MCP Configuration](docs/MCP_CONFIGURATION.md)

## Tech Stack

- .NET 10 / C# 13 / ASP.NET Core Minimal APIs
- EF Core 9 + SQL Server
- ModelContextProtocol SDK (Anthropic/Microsoft)
- xUnit + Moq for testing

## License

MIT
