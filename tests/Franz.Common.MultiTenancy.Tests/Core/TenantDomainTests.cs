using Franz.Common.MultiTenancy.Models;

namespace Franz.Common.MultiTenancy.Tests.Core;

public sealed class TenantDomainTests
{
    [Fact]
    public void Constructor_NormalizesCaseTrailingDotAndInternationalHostnames()
    {
        var domain = new TenantDomain("BÜCHER.Example.");

        Assert.Equal("xn--bcher-kva.example", domain.Host);
        Assert.NotEqual(Guid.Empty, domain.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://tenant.example")]
    [InlineData("tenant.example/path")]
    public void Constructor_RejectsNonHostInput(string host)
    {
        Assert.Throws<ArgumentException>(() => new TenantDomain(host));
    }

    [Fact]
    public void TenantExecutionContext_RejectsInactiveTenant()
    {
        var tenant = new TenantInfo(Guid.NewGuid(), "tenant") { Status = TenantStatus.Suspended };

        Assert.Throws<ArgumentException>(() =>
            new TenantExecutionContext(tenant, null, TenantResolutionSource.Host, DateTimeOffset.UtcNow));
    }
}
