using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.Tests.Infrastructure.Authentication;

internal sealed class TestAuthenticationOptions : AuthenticationSchemeOptions
{
    public bool Authenticate { get; set; }
}

/// <summary>
/// Test-only authentication handler. It deliberately delegates principal construction
/// to <see cref="TestPrincipalFactory"/> so authentication mechanics and identity data
/// can be debugged independently.
/// </summary>
internal sealed class TestAuthenticationHandler(
    IOptionsMonitor<TestAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<TestAuthenticationOptions>(options, logger, encoder)
{
    public const string SchemeName = "FranzGrpcIntegrationTest";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Options.Authenticate)
            return Task.FromResult(AuthenticateResult.NoResult());

        var principal = TestPrincipalFactory.CreateAuthenticatedPrincipal();
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
