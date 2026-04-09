using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ReportTree.Server.Persistance.Relational;

namespace ReportTree.Server.Migrations.SqlServer;

public sealed class SqlServerMigrationsDbContext : AppDbContext
{
    public SqlServerMigrationsDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
}

public sealed class SqlServerMigrationsDbContextFactory : IDesignTimeDbContextFactory<SqlServerMigrationsDbContext>
{
    public SqlServerMigrationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=reporttree_design;Trusted_Connection=True;TrustServerCertificate=True";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new SqlServerMigrationsDbContext(optionsBuilder.Options);
    }
}
