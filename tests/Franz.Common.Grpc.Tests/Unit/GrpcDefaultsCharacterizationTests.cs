using Franz.Common.Grpc.Abstractions;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.Hosting.Security;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcDefaultsCharacterizationTests
{
    [Fact]
    public async Task AddFranzGrpcDefaults_RegistersFailClosedAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcDefaults();

        await using var provider = services.BuildServiceProvider();
        var authorization = provider.GetRequiredService<IFranzAuthorizationService>();

        Assert.IsType<DenyAllAuthorizationService>(authorization);

        await Assert.ThrowsAsync<RpcException>(() =>
            authorization.AuthorizeAsync(GrpcCallContext.CreateClient("tests.Service", "Call")));
    }
}
