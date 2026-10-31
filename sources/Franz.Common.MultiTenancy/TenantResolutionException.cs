namespace Franz.Common.MultiTenancy;

public sealed class TenantResolutionException : Exception
{
    public TenantResolutionException(string message) : base(message) { }
    public TenantResolutionException(string message, Exception innerException) : base(message, innerException) { }
}
