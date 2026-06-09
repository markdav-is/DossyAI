using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DossyAI.Core.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DossyAiDbContext>
{
    public DossyAiDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DossyAiDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=DossyAiDb;Trusted_Connection=true;");
        return new DossyAiDbContext(optionsBuilder.Options);
    }
}
