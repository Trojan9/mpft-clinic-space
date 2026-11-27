using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicSpace.Infrastructure.Persistence;

/// <summary>
/// Lets the dotnet-ef tool create the context to add migrations or script them, without starting
/// the API. The connection string is only used by commands that talk to a database.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ClinicSpace")
            ?? "Server=localhost,1433;Database=ClinicSpace;User Id=sa;Password=Dev_Only_Passw0rd!;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connectionString).Options;
        return new AppDbContext(options, TimeProvider.System);
    }
}
