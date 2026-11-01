using Franz.Common.Http.Authentication.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.MultiTenancy.Tests.Http;

public sealed class JwtBearerConfigurationTests
{
    [Fact]
    public void ConfigureJwtBearer_RejectsMissingConfigurationSection()
    {
        var services = new ServiceCollection();
        var builder = services.AddFranzAuthentication();

        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.ConfigureJwtBearer(new ConfigurationBuilder().Build()));

        Assert.Contains("JwtSettings", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "api", "01234567890123456789012345678901", "Issuer")]
    [InlineData("issuer", "", "01234567890123456789012345678901", "Audience")]
    [InlineData("issuer", "api", "", "SigningKey")]
    public void ConfigureJwtBearer_RejectsMissingRequiredSecuritySettings(
        string issuer,
        string audience,
        string signingKey,
        string expectedSetting)
    {
        var configuration = CreateConfiguration(issuer, audience, signingKey);
        var builder = new ServiceCollection().AddFranzAuthentication();

        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.ConfigureJwtBearer(configuration));

        Assert.Contains(expectedSetting, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureJwtBearer_RejectsSigningKeysShorterThan256Bits()
    {
        var configuration = CreateConfiguration("issuer", "api", "short");
        var builder = new ServiceCollection().AddFranzAuthentication();

        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.ConfigureJwtBearer(configuration));

        Assert.Contains("at least 32 UTF-8 bytes", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigureJwtBearer_RequiresIssuerAudienceLifetimeAndSignatureValidation()
    {
        var configuration = CreateConfiguration(
            "https://identity.example.test",
            "franz-api",
            "01234567890123456789012345678901");
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddFranzAuthentication().ConfigureJwtBearer(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        var validation = options.TokenValidationParameters;

        Assert.True(validation.ValidateIssuer);
        Assert.Equal("https://identity.example.test", validation.ValidIssuer);
        Assert.True(validation.ValidateAudience);
        Assert.Equal("franz-api", validation.ValidAudience);
        Assert.True(validation.ValidateLifetime);
        Assert.True(validation.RequireExpirationTime);
        Assert.True(validation.RequireSignedTokens);
        Assert.True(validation.ValidateIssuerSigningKey);
        Assert.NotNull(validation.IssuerSigningKey);
        Assert.Equal(TimeSpan.Zero, validation.ClockSkew);
    }

    private static IConfiguration CreateConfiguration(string issuer, string audience, string signingKey)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = issuer,
                ["JwtSettings:Audience"] = audience,
                ["JwtSettings:SigningKey"] = signingKey
            })
            .Build();
}
