using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ReportTree.Server.Persistance.Relational;

public class DesignTimeAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();

        var configuredProvider = configuration["Database:Provider"];
        var provider = RelationalDatabaseProvider.IsLiteDb(configuredProvider)
            ? RelationalDatabaseProvider.SqliteProvider
            : RelationalDatabaseProvider.Normalize(configuredProvider ?? RelationalDatabaseProvider.SqliteProvider);

        var connectionString = configuration["Database:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = provider == RelationalDatabaseProvider.SqliteProvider
                ? "Data Source=reporttree.design.db"
                : throw new InvalidOperationException("Database:ConnectionString must be supplied for non-Sqlite design-time migrations.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        RelationalDatabaseProvider.Configure(optionsBuilder, provider, connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}