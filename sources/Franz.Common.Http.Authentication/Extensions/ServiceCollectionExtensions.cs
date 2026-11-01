#nullable enable

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Franz.Common.Http.Authentication.Extensions;

public sealed class FranzSecurityBuilder
{
    public IServiceCollection Services { get; }

    internal FranzSecurityBuilder(IServiceCollection services)
    {
        Services = services;
    }
}

public static class FranzAuthenticationExtensions
{
    /// <summary>
    /// Entry point to register JWT authentication in Franz style.
    /// </summary>
    public static FranzSecurityBuilder AddFranzAuthentication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return new FranzSecurityBuilder(services);
    }

    /// <summary>
    /// Configures JWT bearer authentication using validated symmetric signing settings.
    /// Issuer, audience, and a signing key of at least 256 bits are mandatory.
    /// </summary>
    public static FranzSecurityBuilder ConfigureJwtBearer(
        this FranzSecurityBuilder builder,
        IConfiguration configuration,
        string sectionName = "JwtSettings")
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(sectionName))
            throw new ArgumentException("A JWT configuration section name is required.", nameof(sectionName));

        var section = configuration.GetSection(sectionName);
        var jwtSettings = section.Get<JwtSettings>()
            ?? throw new InvalidOperationException($"JWT configuration section '{sectionName}' is missing or invalid.");

        var issuer = RequireValue(jwtSettings.Issuer, nameof(JwtSettings.Issuer), sectionName);
        var audience = RequireValue(jwtSettings.Audience, nameof(JwtSettings.Audience), sectionName);
        var signingKey = RequireValue(jwtSettings.SigningKey, nameof(JwtSettings.SigningKey), sectionName);
        var keyBytes = Encoding.UTF8.GetBytes(signingKey);

        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                $"JWT setting '{sectionName}:{nameof(JwtSettings.SigningKey)}' must contain at least 32 UTF-8 bytes.");
        }

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    NameClaimType = "name",
                    RoleClaimType = "role",
                    ClockSkew = TimeSpan.Zero
                };
            });

        return builder;
    }

    private static string RequireValue(string? value, string propertyName, string sectionName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"JWT setting '{sectionName}:{propertyName}' is required.");

        return value;
    }
}

/// <summary>
/// Strongly typed JWT settings for symmetric-signature validation.
/// </summary>
public sealed class JwtSettings
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SigningKey { get; set; } = string.Empty;
}
