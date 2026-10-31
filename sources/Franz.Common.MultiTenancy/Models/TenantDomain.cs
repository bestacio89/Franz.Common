using System.Globalization;

namespace Franz.Common.MultiTenancy.Models;

/// <summary>A hostname mapping with an independent identity and explicit tenant ownership.</summary>
public sealed record TenantDomain
{
    public TenantDomain(string host, bool isPrimary = false)
    {
        Host = NormalizeHost(host);
        IsPrimary = isPrimary;
    }

    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TenantId { get; init; }
    public string Host { get; init; }
    public bool IsPrimary { get; init; }
    public bool IsVerified { get; init; }

    /// <summary>Converts a hostname to its canonical IDNA ASCII representation.</summary>
    public static string NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            throw new ArgumentException("A hostname is required.", nameof(host));
        var value = host.Trim().TrimEnd('.');
        if (value.Contains("://", StringComparison.Ordinal) || value.Contains('/') || value.IndexOf((char)92) >= 0)
            throw new ArgumentException("A hostname must not contain a scheme or path.", nameof(host));
        try { return new IdnMapping().GetAscii(value).ToLowerInvariant(); }
        catch (ArgumentException exception) { throw new ArgumentException("The hostname is not a valid IDN hostname.", nameof(host), exception); }
    }
}
