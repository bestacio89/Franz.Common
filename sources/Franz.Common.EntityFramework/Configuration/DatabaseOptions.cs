using System;

namespace Franz.Common.EntityFramework.Configuration;

/// <summary>Controls how a database target is selected for an execution.</summary>
public enum DatabaseIsolationMode
{
    /// <summary>All executions use the configured database name unchanged.</summary>
    SharedDatabase = 0,
    /// <summary>Resolve a database per validated tenant. Missing tenant context fails closed.</summary>
    TenantDatabase = 1,
    /// <summary>Resolve a database per domain. This mode must be explicitly configured.</summary>
    DomainDatabase = 2
}

public class DatabaseOptions
{
    private string _databaseName = default!;

    /// <summary>Database isolation policy. Defaults to a shared database with no identity fallback.</summary>
    public DatabaseIsolationMode IsolationMode { get; set; } = DatabaseIsolationMode.SharedDatabase;

    public string? ServerName { get; set; }

    public string DatabaseName
    {
        get => _databaseName;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("DatabaseName cannot be null or empty", nameof(DatabaseName));
            _databaseName = value;
        }
    }

    public string? UserName { get; set; }
    public string? Password { get; set; }
    public uint? Port { get; set; }
    public string? SslMode { get; set; }

    /// <summary>Enables EF parameter/value logging. Keep disabled in production unless explicitly required.</summary>
    public bool EnableSensitiveDataLogging { get; set; }

    public void ValidateConnectionSettings()
    {
        if (string.IsNullOrWhiteSpace(DatabaseName))
            throw new InvalidOperationException("DatabaseName must be specified.");
        if (string.IsNullOrWhiteSpace(ServerName))
            throw new InvalidOperationException("ServerName should be provided.");
    }
}
