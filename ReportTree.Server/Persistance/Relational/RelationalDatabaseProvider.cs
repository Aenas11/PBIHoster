using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyModel;
using System.Reflection;

namespace ReportTree.Server.Persistance.Relational;

public static class RelationalDatabaseProvider
{
    public const string LiteDbProvider = "LiteDb";
    public const string SqliteProvider = "Sqlite";
    public const string SqlServerProvider = "SqlServer";
    public const string PostgreSqlProvider = "PostgreSql";

    public const string SqliteMigrationsAssembly = "ReportTree.Server";
    public const string SqlServerMigrationsAssembly = "ReportTree.Server.Migrations.SqlServer";
    public const string PostgreSqlMigrationsAssembly = "ReportTree.Server.Migrations.PostgreSql";

    public static bool IsLiteDb(string? provider)
    {
        return string.Equals(provider?.Trim(), LiteDbProvider, StringComparison.OrdinalIgnoreCase);
    }

    public static bool SupportsCommittedMigrations(string? provider)
    {
        var normalized = Normalize(provider);
        return normalized == SqliteProvider || normalized == SqlServerProvider || normalized == PostgreSqlProvider;
    }

    public static string GetMigrationsAssemblyName(string provider)
    {
        return Normalize(provider) switch
        {
            SqliteProvider => SqliteMigrationsAssembly,
            SqlServerProvider => SqlServerMigrationsAssembly,
            PostgreSqlProvider => PostgreSqlMigrationsAssembly,
            _ => throw new InvalidOperationException($"Unsupported database provider '{provider}'.")
        };
    }

    public static bool IsMigrationsAssemblyAvailable(string provider)
    {
        var assemblyName = GetMigrationsAssemblyName(provider);
        if (string.Equals(assemblyName, SqliteMigrationsAssembly, StringComparison.Ordinal))
        {
            return true;
        }

        try
        {
            var dependencyContext = DependencyContext.Default;
            if (dependencyContext?.RuntimeLibraries.Any(x => string.Equals(x.Name, assemblyName, StringComparison.OrdinalIgnoreCase)) == true)
            {
                return true;
            }

            Assembly.Load(new AssemblyName(assemblyName));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string Normalize(string? provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new InvalidOperationException($"Database provider is required. Supported providers are {LiteDbProvider}, {SqliteProvider}, {SqlServerProvider}, {PostgreSqlProvider}.");
        }

        var trimmedProvider = provider.Trim();
        if (string.Equals(trimmedProvider, LiteDbProvider, StringComparison.OrdinalIgnoreCase))
        {
            return LiteDbProvider;
        }

        if (string.Equals(trimmedProvider, SqliteProvider, StringComparison.OrdinalIgnoreCase))
        {
            return SqliteProvider;
        }

        if (string.Equals(trimmedProvider, SqlServerProvider, StringComparison.OrdinalIgnoreCase))
        {
            return SqlServerProvider;
        }

        if (string.Equals(trimmedProvider, PostgreSqlProvider, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(trimmedProvider, "Postgres", StringComparison.OrdinalIgnoreCase))
        {
            return PostgreSqlProvider;
        }

        throw new InvalidOperationException($"Unsupported database provider '{provider}'. Supported providers are {LiteDbProvider}, {SqliteProvider}, {SqlServerProvider}, {PostgreSqlProvider}.");
    }

    public static void Configure(DbContextOptionsBuilder optionsBuilder, string provider, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database:ConnectionString is required for relational providers.");
        }

        var normalizedProvider = Normalize(provider);
        if (normalizedProvider == LiteDbProvider)
        {
            throw new InvalidOperationException("LiteDb does not use EF Core relational configuration.");
        }

        var migrationsAssembly = GetMigrationsAssemblyName(normalizedProvider);

        switch (normalizedProvider)
        {
            case SqliteProvider:
                optionsBuilder.UseSqlite(connectionString, options => options.MigrationsAssembly(migrationsAssembly));
                break;
            case SqlServerProvider:
                optionsBuilder.UseSqlServer(connectionString, options => options.MigrationsAssembly(migrationsAssembly));
                break;
            case PostgreSqlProvider:
                optionsBuilder.UseNpgsql(connectionString, options => options.MigrationsAssembly(migrationsAssembly));
                break;
            default:
                throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
        }
    }
}