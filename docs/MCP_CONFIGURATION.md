# MCP Client Configuration

## Claude Desktop

Add to `~/Library/Application Support/Claude/claude_desktop_config.json` (macOS) or `%APPDATA%\Claude\claude_desktop_config.json` (Windows):

```json
{
  "mcpServers": {
    "dossyai": {
      "command": "npx",
      "args": ["-y", "@modelcontextprotocol/server-http", "http://localhost:5000/mcp"],
      "env": {
        "MCP_API_KEY": "dossyai-key-dev"
      }
    }
  }
}
```

Or if using the HTTP transport directly:

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

## VS Code (Continue extension)

In `.continue/config.json`:

```json
{
  "mcpServers": [
    {
      "name": "dossyai",
      "transport": {
        "type": "http",
        "url": "http://localhost:5000/mcp",
        "headers": {
          "x-dossyai-key": "dossyai-key-dev"
        }
      }
    }
  ]
}
```

## Cursor

In Cursor settings → MCP:

```json
{
  "dossyai": {
    "url": "http://localhost:5000/mcp",
    "headers": {
      "x-dossyai-key": "dossyai-key-dev"
    }
  }
}
```

## ChatGPT (Custom GPT Actions)

Use the `/health` endpoint to verify connectivity and configure the OpenAPI schema pointing at your DossyAI deployment.
