namespace Franz.Common.MultiTenancy;

public sealed class TenantIsolationException : InvalidOperationException
{
    public TenantIsolationException(string message) : base(message) { }
}
