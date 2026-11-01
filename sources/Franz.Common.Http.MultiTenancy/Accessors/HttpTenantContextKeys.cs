namespace Franz.Common.Http.MultiTenancy.Accessors;

internal static class HttpTenantContextKeys
{
    internal static readonly object TenantId = new();
    internal static readonly object DomainId = new();
    internal static readonly object ExecutionContext = new();
}
