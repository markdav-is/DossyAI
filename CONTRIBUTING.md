# Contributing to DossyAI

## Development Setup

1. Fork and clone the repository
2. Install .NET 10 SDK
3. Run `dotnet restore`
4. Make your changes
5. Run tests: `dotnet test`
6. Submit a PR

## Project Structure

- `src/DossyAI.Core` — domain models and business logic
- `src/DossyAI.Mcp` — MCP tools and authentication
- `src/DossyAI.Api` — ASP.NET Core host
- `tests/DossyAI.Tests` — unit tests

## Code Style

- Follow C# naming conventions
- Use `async`/`await` throughout
- Keep tools thin — business logic belongs in `MemoryService`
- All public methods should be nullable-annotated

## Testing

Run the full test suite:

```bash
dotnet test --verbosity normal
```

## Pull Request Guidelines

- Include tests for new features
- Update docs if adding/changing MCP tools
- Keep commits focused
