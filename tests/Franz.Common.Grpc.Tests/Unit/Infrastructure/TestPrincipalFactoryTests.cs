using Franz.Common.Grpc.Tests.Infrastructure.Authentication;
using System.Security.Claims;

namespace Franz.Common.Grpc.Tests.Unit.Infrastructure;

public sealed class TestPrincipalFactoryTests
{
    [Fact]
    public void CreateAuthenticatedPrincipal_PreservesTrustBoundaryTestIdentities()
    {
        var principal = TestPrincipalFactory.CreateAuthenticatedPrincipal();
        var identities = principal.Identities.ToArray();

        Assert.Collection(
            identities,
            unauthenticated =>
            {
                Assert.False(unauthenticated.IsAuthenticated);
                Assert.Equal("untrusted-user", unauthenticated.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            },
            authenticatedWithoutSubject =>
            {
                Assert.True(authenticatedWithoutSubject.IsAuthenticated);
                Assert.Null(authenticatedWithoutSubject.FindFirst(ClaimTypes.NameIdentifier));
            },
            authenticatedWithSubject =>
            {
                Assert.True(authenticatedWithSubject.IsAuthenticated);
                Assert.Equal("trusted-user", authenticatedWithSubject.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            });
    }
}
