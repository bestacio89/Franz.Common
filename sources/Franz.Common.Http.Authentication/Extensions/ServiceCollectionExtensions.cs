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
    // Avoid duplicating registrations if needed
    return new FranzSecurityBuilder(services);
  }

  /// <summary>
  /// Configures JWT bearer authentication using the provided configuration section.
  /// </summary>
  public static FranzSecurityBuilder ConfigureJwtBearer(
      this FranzSecurityBuilder builder,
      IConfiguration configuration,
      string sectionName = "JwtSettings")
  {
    var jwtSettings = configuration.GetSection(sectionName).Get<JwtSettings>()!;

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
          options.RequireHttpsMetadata = true;
          options.SaveToken = false;

          options.TokenValidationParameters = new TokenValidationParameters
          {
            ValidateIssuer = !string.IsNullOrEmpty(jwtSettings.Issuer),
            ValidateAudience = !string.IsNullOrEmpty(jwtSettings.Audience),
            ValidateLifetime = true,
            ValidateIssuerSigningKey = !string.IsNullOrEmpty(jwtSettings.SigningKey),
            IssuerSigningKey = string.IsNullOrEmpty(jwtSettings.SigningKey)
                      ? null
                      : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.Zero
          };
        });

    return builder;
  }
}

/// <summary>
/// Strongly typed JWT settings for configuration.
/// </summary>
public class JwtSettings
{
  public string Issuer { get; set; } = "";
  public string Audience { get; set; } = "";
  public string SigningKey { get; set; } = "";
}