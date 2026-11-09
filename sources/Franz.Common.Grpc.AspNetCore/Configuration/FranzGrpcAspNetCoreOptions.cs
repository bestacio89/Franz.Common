using System.Security.Claims;

namespace Franz.Common.Grpc.AspNetCore.Configuration;

/// <summary>
/// Configures the trusted identity contract between ASP.NET Core authentication
/// and the Franz gRPC execution context.
/// </summary>
public sealed class FranzGrpcAspNetCoreOptions
{
    /// <summary>
    /// Claim type used as the authenticated caller's stable subject identifier.
    /// The claim is read only from an authenticated server-side principal.
    /// </summary>
    public string UserIdClaimType { get; set; } = ClaimTypes.NameIdentifier;
}
