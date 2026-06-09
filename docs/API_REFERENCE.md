# DossyAI MCP API Reference

All tools are exposed via the MCP protocol at `/mcp`.

## store_context

Store a piece of intelligence with semantic embedding.

**Parameters:**
- `content` (string, required): The text content to store
- `category` (string, required): Category label (e.g., "meeting-notes", "decisions")
- `metadata` (object, optional): Additional key-value metadata

**Response:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "created_at": "2024-01-15T10:30:00Z",
  "embedding_dimensions": 1536,
  "status": "stored",
  "category": "meeting-notes",
  "content_hash": "abc123..."
}
```

## find_related

Find memories semantically related to the given query.

**Parameters:**
- `query` (string, required): The search query text
- `limit` (int, optional, default: 10): Maximum results to return
- `minSimilarity` (float, optional, default: 0.7): Minimum cosine similarity threshold

**Response:**
```json
{
  "count": 2,
  "results": [
    {
      "id": "3fa85f64-...",
      "content": "Meeting notes from Q1 planning...",
      "category": "meeting-notes",
      "similarity_score": 0.92,
      "created_at": "2024-01-15T10:30:00Z",
      "status": "active",
      "metadata": "{}"
    }
  ]
}
```

## retrieve_by_category

Retrieve memories filtered by category.

**Parameters:**
- `category` (string, required): Category to filter by
- `domainFilter` (string, optional): Filter by domain in metadata
- `skip` (int, optional, default: 0): Pagination offset
- `take` (int, optional, default: 20): Page size

**Response:**
```json
{
  "category": "meeting-notes",
  "count": 5,
  "skip": 0,
  "take": 20,
  "items": [...]
}
```

## list_memory

List stored memories with optional filtering.

**Parameters:**
- `category` (string, optional): Filter by category
- `createdBy` (string, optional): Filter by creator
- `skip` (int, optional, default: 0)
- `take` (int, optional, default: 20)

**Response:**
```json
{
  "total": 42,
  "skip": 0,
  "take": 20,
  "items": [...]
}
```

## update_memory

Update an existing memory.

**Parameters:**
- `id` (Guid, required): Memory ID
- `newContent` (string, optional): Updated content
- `newMetadata` (object, optional): Metadata to merge in
- `status` (string, optional): New status value

**Response:**
```json
{
  "id": "3fa85f64-...",
  "updated_at": "2024-01-15T11:00:00Z",
  "status": "active",
  "content_hash": "def456..."
}
```

## delete_memory

Soft-delete (archive) a memory.

**Parameters:**
- `id` (Guid, required): Memory ID
- `reason` (string, optional): Reason for deletion

**Response:**
```json
{
  "id": "3fa85f64-...",
  "status": "archived",
  "archived_at": "2024-01-15T11:00:00Z",
  "reason": "outdated"
}
```

## get_memory_stats

Get statistics about the memory store.

**Parameters:** None

**Response:**
```json
{
  "total_memories": 150,
  "active_memories": 140,
  "archived_memories": 10,
  "by_category": {
    "meeting-notes": 50,
    "decisions": 30
  },
  "by_created_by": {
    "agent": 150
  },
  "oldest_memory": "2024-01-01T00:00:00Z",
  "newest_memory": "2024-01-15T10:30:00Z"
}
```
