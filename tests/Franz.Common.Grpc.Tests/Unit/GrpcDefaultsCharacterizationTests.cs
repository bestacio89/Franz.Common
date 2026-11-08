using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.Hosting.NoOp;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcDefaultsCharacterizationTests
{
    [Fact]
    public async Task AddFranzGrpcDefaults_RegistersAllowAllAuthorizationServiceWhenExplicitlyCalled()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcDefaults();

        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IFranzAuthorizationService>();

        Assert.IsType<NoOpAuthorizationService>(authorization);

        // Characterization only: this no-op implementation accepts every call.
        await authorization.AuthorizeAsync(GrpcCallContext.CreateClient("tests.Service", "Call"));
    }
}
