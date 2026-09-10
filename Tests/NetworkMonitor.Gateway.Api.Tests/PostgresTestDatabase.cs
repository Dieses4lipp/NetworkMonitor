using Microsoft.EntityFrameworkCore;
using NetworkMonitor.Infrastructure.Data.Context;
using Npgsql;

namespace NetworkMonitor.Gateway.Api.Tests;

/// <summary>
/// Creates a throwaway schema on a real PostgreSQL server for a single test.
/// The password arrives in its own variable because connection-string parsing mangles
/// passwords that contain quotes or spaces.
/// </summary>
public sealed class PostgresTestDatabase
{
    public const string ConnectionStringVariable = "NETWORKMONITOR_TEST_DB";
    public const string PasswordVariable = "NETWORKMONITOR_TEST_DB_PASSWORD";

    private readonly string _connectionString;

    public PostgresTestDatabase()
    {
        var configured = Environment.GetEnvironmentVariable(ConnectionStringVariable)
            ?? throw new InvalidOperationException(
                $"{ConnectionStringVariable} is not set. Point it at a throwaway PostgreSQL database.");

        var builder = new NpgsqlConnectionStringBuilder(configured);

        var password = Environment.GetEnvironmentVariable(PasswordVariable);
        if (!string.IsNullOrEmpty(password))
            builder.Password = password;

        if (builder.Database is null || !builder.Database.Contains("test", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"Refusing to run against database '{builder.Database}'. The database name must contain 'test'.");

        _connectionString = builder.ConnectionString;

        using var context = CreateContext();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
    }

    public NetworkMonitorDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NetworkMonitorDbContext>()
            .UseNpgsql(_connectionString)
            .Options;

        return new NetworkMonitorDbContext(options);
    }
}
