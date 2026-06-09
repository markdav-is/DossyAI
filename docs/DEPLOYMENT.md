# Deployment Guide

## Docker

### Build and run locally

```bash
docker compose -f docker/docker-compose.yml up -d
```

The API will be available at `http://localhost:5000`.

### Environment variables

Set in `docker-compose.yml` or pass via `-e`:

```bash
docker run -e DossyAi__Mcp__ApiKey=my-secret-key \
           -e DossyAi__Database__ConnectionString="..." \
           -p 5000:5000 ghcr.io/your-org/dossyai:latest
```

## Azure Container Instance

```bash
az container create \
  --resource-group my-rg \
  --name dossyai \
  --image ghcr.io/your-org/dossyai:latest \
  --ports 5000 \
  --environment-variables \
    DossyAi__Database__ConnectionString="Server=..." \
    DossyAi__Mcp__ApiKey="my-secret-key"
```

## Azure App Service

1. Create an App Service (Linux, .NET 10)
2. Set connection string and app settings in Configuration
3. Deploy from GitHub Actions or Azure DevOps

```bash
az webapp create --name dossyai --resource-group my-rg --plan my-plan --runtime "DOTNET|10.0"
az webapp config appsettings set --name dossyai --resource-group my-rg \
  --settings DossyAi__Mcp__ApiKey="my-secret-key"
```
