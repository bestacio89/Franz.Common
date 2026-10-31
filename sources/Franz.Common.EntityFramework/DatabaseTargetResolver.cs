using Franz.Common.EntityFramework.Configuration;
using Franz.Common.MultiTenancy;

namespace Franz.Common.EntityFramework;

/// <summary>Resolves database names using the single provider-neutral isolation policy.</summary>
public static class DatabaseTargetResolver
{
    public const string DatabaseNameToken = "{dbName}";

    public static string ResolveDatabaseName(
        DatabaseOptions options,
        ITenantExecutionContextAccessor? tenantAccessor,
        IDomainContextAccessor? domainAccessor)
    {
        ArgumentNullException.ThrowIfNull(options);
        var configuredName = options.DatabaseName;
        var hasToken = configuredName.Contains(DatabaseNameToken, StringComparison.Ordinal);

        return options.IsolationMode switch
        {
            DatabaseIsolationMode.SharedDatabase => ResolveShared(configuredName, hasToken),
            DatabaseIsolationMode.TenantDatabase => ResolveTenant(configuredName, hasToken, tenantAccessor),
            DatabaseIsolationMode.DomainDatabase => ResolveDomain(configuredName, hasToken, domainAccessor),
            _ => throw new InvalidOperationException($"Unsupported database isolation mode '{options.IsolationMode}'.")
        };
    }

    private static string ResolveShared(string name, bool hasToken)
    {
        if (hasToken)
            throw new InvalidOperationException(
                $"DatabaseName contains '{DatabaseNameToken}', but IsolationMode is SharedDatabase. " +
                "Choose TenantDatabase or explicitly opt in to DomainDatabase.");
        return name;
    }

    private static string ResolveTenant(
        string name,
        bool hasToken,
        ITenantExecutionContextAccessor? accessor)
    {
        if (!hasToken)
            throw new InvalidOperationException(
                $"IsolationMode TenantDatabase requires DatabaseName to contain '{DatabaseNameToken}'.");
        var tenantId = accessor?.Current?.TenantId;
        if (tenantId is null || tenantId == Guid.Empty)
            throw new TenantIsolationException(
                "TenantDatabase routing requires a validated tenant execution context; domain identity is not a fallback.");
        return name.Replace(DatabaseNameToken, tenantId.Value.ToString("D"), StringComparison.Ordinal);
    }

    private static string ResolveDomain(
        string name,
        bool hasToken,
        IDomainContextAccessor? accessor)
    {
        if (!hasToken)
            throw new InvalidOperationException(
                $"IsolationMode DomainDatabase requires DatabaseName to contain '{DatabaseNameToken}'.");
        var domainId = accessor?.GetCurrentDomainId();
        if (domainId is null || domainId == Guid.Empty)
            throw new InvalidOperationException(
                "DomainDatabase routing requires a domain context. Configure SharedDatabase or TenantDatabase otherwise.");
        return name.Replace(DatabaseNameToken, domainId.Value.ToString("D"), StringComparison.Ordinal);
    }
}
