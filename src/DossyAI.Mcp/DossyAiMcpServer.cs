using DossyAI.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace DossyAI.Mcp;

public static class DossyAiMcpServer
{
    public static IServiceCollection AddDossyAiMcp(this IServiceCollection services)
    {
        services.AddMcpServer()
            .WithTools<StoreContextTool>()
            .WithTools<FindRelatedTool>()
            .WithTools<RetrieveByCategoryTool>()
            .WithTools<ListMemoryTool>()
            .WithTools<UpdateMemoryTool>()
            .WithTools<DeleteMemoryTool>()
            .WithTools<GetMemoryStatsTool>();

        return services;
    }
}
