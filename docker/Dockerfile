# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY DossyAI.sln ./
COPY nuget.config ./
COPY src/DossyAI.Core/DossyAI.Core.csproj src/DossyAI.Core/
COPY src/DossyAI.Mcp/DossyAI.Mcp.csproj src/DossyAI.Mcp/
COPY src/DossyAI.Api/DossyAI.Api.csproj src/DossyAI.Api/
COPY tests/DossyAI.Tests/DossyAI.Tests.csproj tests/DossyAI.Tests/

RUN dotnet restore

COPY . .

RUN dotnet publish src/DossyAI.Api/DossyAI.Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DossyAI.Api.dll"]
