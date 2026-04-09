using Microsoft.EntityFrameworkCore;
using ReportTree.Server.Persistance.Relational;
using Xunit;

namespace ReportTree.Server.Tests;

public class RelationalDatabaseProviderTests
{
    [Theory]
    [InlineData("Sqlite", RelationalDatabaseProvider.SqliteProvider)]
    [InlineData("sqlite", RelationalDatabaseProvider.SqliteProvider)]
    [InlineData("SqlServer", RelationalDatabaseProvider.SqlServerProvider)]
    [InlineData("PostgreSql", RelationalDatabaseProvider.PostgreSqlProvider)]
    [InlineData("Postgres", RelationalDatabaseProvider.PostgreSqlProvider)]
    [InlineData("LiteDb", RelationalDatabaseProvider.LiteDbProvider)]
    public void Normalize_ReturnsCanonicalProviderName(string input, string expected)
    {
        var provider = RelationalDatabaseProvider.Normalize(input);
        Assert.Equal(expected, provider);
    }

    [Fact]
    public void Normalize_RejectsUnsupportedProvider()
    {
        var exception = Assert.Throws<InvalidOperationException>(() => RelationalDatabaseProvider.Normalize("MongoDb"));
        Assert.Contains("Unsupported database provider", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RelationalDatabaseProvider.SqliteProvider, "Data Source=test.db", "Microsoft.EntityFrameworkCore.Sqlite")]
    [InlineData(RelationalDatabaseProvider.SqlServerProvider, "Server=(localdb)\\mssqllocaldb;Database=reporttree-tests;Trusted_Connection=True;", "Microsoft.EntityFrameworkCore.SqlServer")]
    [InlineData(RelationalDatabaseProvider.PostgreSqlProvider, "Host=localhost;Database=reporttree-tests;Username=test;Password=test", "Npgsql.EntityFrameworkCore.PostgreSQL")]
    public void Configure_AssignsExpectedEfProvider(string provider, string connectionString, string expectedProviderName)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        RelationalDatabaseProvider.Configure(optionsBuilder, provider, connectionString);

        using var context = new AppDbContext(optionsBuilder.Options);
        Assert.Equal(expectedProviderName, context.Database.ProviderName);
    }

    [Fact]
    public void Configure_RejectsLiteDbProvider()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var exception = Assert.Throws<InvalidOperationException>(() =>
            RelationalDatabaseProvider.Configure(optionsBuilder, RelationalDatabaseProvider.LiteDbProvider, "ignored"));

        Assert.Contains("LiteDb does not use EF Core relational configuration", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(RelationalDatabaseProvider.SqliteProvider, true)]
    [InlineData(RelationalDatabaseProvider.SqlServerProvider, true)]
    [InlineData(RelationalDatabaseProvider.PostgreSqlProvider, true)]
    public void SupportsCommittedMigrations_IsScopedToCurrentMigrationSet(string provider, bool expected)
    {
        Assert.Equal(expected, RelationalDatabaseProvider.SupportsCommittedMigrations(provider));
    }

    [Theory]
    [InlineData(RelationalDatabaseProvider.SqliteProvider, RelationalDatabaseProvider.SqliteMigrationsAssembly)]
    [InlineData(RelationalDatabaseProvider.SqlServerProvider, RelationalDatabaseProvider.SqlServerMigrationsAssembly)]
    [InlineData(RelationalDatabaseProvider.PostgreSqlProvider, RelationalDatabaseProvider.PostgreSqlMigrationsAssembly)]
    public void GetMigrationsAssemblyName_ReturnsExpectedAssembly(string provider, string expectedAssembly)
    {
        Assert.Equal(expectedAssembly, RelationalDatabaseProvider.GetMigrationsAssemblyName(provider));
    }

    [Fact]
    public void IsMigrationsAssemblyAvailable_ReturnsTrueForSqliteAssembly()
    {
        Assert.True(RelationalDatabaseProvider.IsMigrationsAssemblyAvailable(RelationalDatabaseProvider.SqliteProvider));
    }
}