using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ReportTree.Server.Persistance.Relational;

namespace ReportTree.Server.Migrations.PostgreSql;

public sealed class PostgreSqlMigrationsDbContext : AppDbContext
{
    public PostgreSqlMigrationsDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}

public sealed class PostgreSqlMigrationsDbContextFactory : IDesignTimeDbContextFactory<PostgreSqlMigrationsDbContext>
{
    public PostgreSqlMigrationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Database=reporttree_design;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new PostgreSqlMigrationsDbContext(optionsBuilder.Options);
    }
}
