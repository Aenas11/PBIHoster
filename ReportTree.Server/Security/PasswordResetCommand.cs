using LiteDB;
using Microsoft.Extensions.Configuration;
using ReportTree.Server.Models;

namespace ReportTree.Server.Security;

public static class PasswordResetCommand
{
    public static bool TryRun(string[] args)
    {
        var shouldReset = args.Contains("--reset-password", StringComparer.OrdinalIgnoreCase);
        var shouldListAdmins = args.Contains("--list-admins", StringComparer.OrdinalIgnoreCase);

        if (!shouldReset && !shouldListAdmins)
        {
            return false;
        }

        var dbConnectionString = GetOption(args, "--db");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = string.IsNullOrWhiteSpace(dbConnectionString)
            ? configuration["LiteDb:ConnectionString"] ?? "Filename=reporttree.db;Connection=shared"
            : dbConnectionString;

        connectionString = ResolveFilenamePath(connectionString);

        using var db = new LiteDatabase(connectionString);
        var users = db.GetCollection<AppUser>("users");

        if (shouldListAdmins)
        {
            var admins = users.Find(x => x.Roles.Contains("Admin")).Select(x => x.Username).Distinct().ToList();
            if (admins.Count == 0)
            {
                Console.WriteLine("No admin users found.");
            }
            else
            {
                Console.WriteLine("Admin users:");
                foreach (var admin in admins)
                {
                    Console.WriteLine($" - {admin}");
                }
            }

            return true;
        }

        var username = GetOption(args, "--username");
        var newPassword = GetOption(args, "--new-password");
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(newPassword))
        {
            Console.Error.WriteLine("Missing required arguments.");
            Console.Error.WriteLine("Usage: --reset-password --username <username> --new-password <password> [--db <connectionString>]");
            Environment.ExitCode = 1;
            return true;
        }

        var policy = new PasswordPolicy();
        configuration.GetSection("Security:PasswordPolicy").Bind(policy);

        var validator = new PasswordValidator(policy);
        var (isValid, errors) = validator.Validate(newPassword);
        if (!isValid)
        {
            Console.Error.WriteLine("Password does not satisfy policy:");
            foreach (var error in errors)
            {
                Console.Error.WriteLine($" - {error}");
            }

            Environment.ExitCode = 1;
            return true;
        }

        var user = users.FindOne(x => x.Username == username);

        if (user == null)
        {
            Console.Error.WriteLine($"User '{username}' was not found.");
            Environment.ExitCode = 1;
            return true;
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        users.Upsert(user);

        var lockouts = db.GetCollection<AccountLockout>("account_lockouts");
        lockouts.DeleteMany(x => x.Username == username);

        Console.WriteLine($"Password reset successful for user '{username}'.");
        return true;
    }

    private static string? GetOption(string[] args, string optionName)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], optionName, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    private static string ResolveFilenamePath(string connectionString)
    {
        var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (!segment.StartsWith("Filename=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var filePath = segment["Filename=".Length..];
            if (Path.IsPathRooted(filePath))
            {
                return connectionString;
            }

            var absolute = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), filePath));
            segments[i] = $"Filename={absolute}";
            return string.Join(';', segments);
        }

        return connectionString;
    }
}
