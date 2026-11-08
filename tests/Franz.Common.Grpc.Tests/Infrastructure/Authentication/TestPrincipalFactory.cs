using System.Security.Claims;

namespace Franz.Common.Grpc.Tests.Infrastructure.Authentication;

/// <summary>
/// Creates deterministic principals for authentication and identity-trust integration tests.
/// </summary>
internal static class TestPrincipalFactory
{
    public static ClaimsPrincipal CreateAuthenticatedPrincipal()
        => new(
        [
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "untrusted-user")
            ]),
            new ClaimsIdentity(authenticationType: "AuthenticatedWithoutSubject"),
            new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "trusted-user")
            ],
            TestAuthenticationHandler.SchemeName)
        ]);
}
