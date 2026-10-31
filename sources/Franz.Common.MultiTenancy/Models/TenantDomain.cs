using System.Globalization;

namespace Franz.Common.MultiTenancy.Models;

/// <summary>A hostname mapping with an independent identity and explicit tenant ownership.</summary>
public sealed record TenantDomain
{
    public TenantDomain(string host, bool isPrimary = false)
    {
        if (string.IsNullOrWhiteSpace(host)) throw new ArgumentException("A hostname is required.", nameof(host));
        var value = host.Trim().TrimEnd('.');
        if (value.Contains("://", StringComparison.Ordinal) || value.Contains('/') || value.Contains('\\'))
            throw new ArgumentException("A hostname must not contain a scheme or path.", nameof(host));
        try { Host = new IdnMapping().GetAscii(value).ToLowerInvariant(); }
        catch (ArgumentException exception) { throw new ArgumentException("The hostname is not a valid IDN hostname.", nameof(host), exception); }
        IsPrimary = isPrimary;
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TenantId { get; init; }
    public string Host { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsVerified { get; init; }
}
