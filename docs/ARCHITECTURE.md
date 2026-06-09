# DossyAI Architecture

## Overview

DossyAI is a persistent memory system for AI agents, exposing a Model Context Protocol (MCP) server that allows AI agents to store, search, and retrieve contextual information across sessions.

## Project Layers

### DossyAI.Core

The core library containing domain models, database access, and business logic:

- **Models/**: `Memory`, `MemoryStats`, `MemorySearchRequest`, `MemoryResponse`, `MemorySearchResult`
- **Data/**: `DossyAiDbContext` (EF Core), `DesignTimeDbContextFactory`
- **Services/**: `IMemoryService`, `MemoryService`, `IEmbeddingService`, `OllamaEmbeddingService`, `OpenRouterEmbeddingService`, `MetadataExtractor`

### DossyAI.Mcp

The MCP server layer, exposing tools via the ModelContextProtocol SDK:

- **Tools/**: 7 MCP tool classes (StoreContext, FindRelated, RetrieveByCategory, ListMemory, UpdateMemory, DeleteMemory, GetMemoryStats)
- **Authentication/**: `ApiKeyAuthenticationHandler`
- **DossyAiMcpServer.cs**: Extension method to register MCP services

### DossyAI.Api

The ASP.NET Core host:

- **Program.cs**: DI setup, middleware pipeline, health endpoint, MCP endpoint

## Data Flow

```
AI Agent (Claude, etc.)
        |
        | MCP Protocol (HTTP/SSE)
        v
DossyAI.Api (/mcp endpoint)
        |
        | ApiKey Authentication
        v
MCP Tool (e.g., StoreContextTool)
        |
        v
IMemoryService
        |
    +---+---+
    |       |
    v       v
IEmbedding  MetadataExtractor
Service     (LLM extraction)
    |
    v
DossyAiDbContext
        |
        v
SQL Server (Memories table)
```

## Embedding Pipeline

1. User/agent calls `store_context` with text content
2. `MemoryService` calls `IEmbeddingService.GetEmbeddingAsync()` to convert text to a float vector
3. `MetadataExtractor` uses an LLM to extract structured metadata (type, domain, stakeholders, etc.)
4. The `Memory` entity is persisted with the embedding stored as JSON in `nvarchar(max)`
5. For `find_related`, cosine similarity is computed in-memory across all non-archived memories

## Storage

Embeddings are stored as JSON-serialized `float[]` in SQL Server `nvarchar(max)` columns using an EF Core value converter. This avoids requiring SQL Server 2025 vector type support while remaining fully functional.
