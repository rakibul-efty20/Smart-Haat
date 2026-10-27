using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SmartHaat.Infrastructure.Persistence;

public sealed class SmartHaatDesignTimeFactory : IDesignTimeDbContextFactory<SmartHaatDbContext>
{
    public SmartHaatDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__SmartHaat")
            ?? "Server=(localdb)\\mssqllocaldb;Database=SmartHaat;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        return new SmartHaatDbContext(new DbContextOptionsBuilder<SmartHaatDbContext>().UseSqlServer(connection).Options);
    }
}
