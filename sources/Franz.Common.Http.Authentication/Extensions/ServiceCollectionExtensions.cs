using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;

namespace Franz.Common.Http.Authentication.Extensions;
public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddFranzAuthentication(this IServiceCollection services)
  {
    services
      .AddSwaggerGen(options =>
      {
        options.AddSecurityDefinition(name: "Bearer", securityScheme: new OpenApiSecurityScheme
        {
          Name = "Authorization",
          Description = "Enter JWT",
          In = ParameterLocation.Header,
          Type = SecuritySchemeType.Http,
          BearerFormat = "JWT",
          Scheme = "Bearer"
        });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
          {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{}
          }
        });
      })
      .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        options.SaveToken = false;
        options.TokenValidationParameters = new TokenValidationParameters()
        {
          ValidateIssuer = false,
          ValidateAudience = false,
          ValidateLifetime = false,
          ValidateIssuerSigningKey = false,
          RequireSignedTokens = false,
          NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname",
          RoleClaimType = "role",
          SignatureValidator = (token, parameters) => new JwtSecurityToken(token),
        };
      });

    return services;
  }
}
