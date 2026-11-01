using System.Net;
using Franz.Common.Http.Extensions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class HttpBoundaryConfigurationTests
{
    [Fact]
    public void AddDefaultCors_DoesNotAllowCrossOriginRequestsWhenNoOriginsAreConfigured()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddFranzHttp().AddDefaultCors(configuration);

        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>()
            .Value.GetPolicy(new CorsOptions().DefaultPolicyName);

        Assert.NotNull(policy);
        Assert.Empty(policy.Origins);
        Assert.False(policy.SupportsCredentials);
        Assert.DoesNotContain("*", policy.Origins);
    }

    [Fact]
    public void AddDefaultCors_AllowsOnlyExplicitExactOrigins()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors"] = "https://app.example.test;https://admin.example.test/"
            })
            .Build();

        services.AddFranzHttp().AddDefaultCors(configuration);

        using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<IOptions<CorsOptions>>()
            .Value.GetPolicy(CorsOptions.DefaultPolicyName);

        Assert.NotNull(policy);
        Assert.Equal(
            new[] { "https://app.example.test", "https://admin.example.test" },
            policy.Origins);
        Assert.True(policy.SupportsCredentials);
        Assert.False(policy.AllowAnyOrigin);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.test")]
    [InlineData("https://example.test/api")]
    [InlineData("https://user:password@example.test")]
    [InlineData("javascript:alert(1)")]
    public void AddDefaultCors_RejectsNonExactOrInvalidOrigins(string origin)
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Cors"] = origin })
            .Build();

        var builder = services.AddFranzHttp();

        Assert.Throws<InvalidOperationException>(() => builder.AddDefaultCors(configuration));
    }

    [Fact]
    public void AddForwardedHeaders_PreservesDefaultTrustedProxyRestrictions()
    {
        var services = new ServiceCollection();

        services.AddFranzHttp().AddForwardedHeaders();

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.NotEmpty(options.KnownProxies);
        Assert.Equal(
            ForwardedHeaders.XForwardedFor |
            ForwardedHeaders.XForwardedProto |
            ForwardedHeaders.XForwardedHost,
            options.ForwardedHeaders);
    }

    [Fact]
    public void AddForwardedHeaders_AllowsExplicitTrustedProxyConfiguration()
    {
        var trustedProxy = IPAddress.Parse("10.20.30.40");
        var services = new ServiceCollection();

        services.AddFranzHttp().AddForwardedHeaders(options => options.KnownProxies.Add(trustedProxy));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ForwardedHeadersOptions>>().Value;

        Assert.Contains(trustedProxy, options.KnownProxies);
    }
}
