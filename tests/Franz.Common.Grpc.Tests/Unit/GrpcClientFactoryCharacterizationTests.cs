using Franz.Common.Grpc.Client;
using Franz.Common.Grpc.Configuration;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcClientFactoryCharacterizationTests
{
    [Fact]
    public void CreateChannel_CreatesANewChannelForEveryRequest()
    {
        var options = Options.Create(new FranzGrpcClientOptions
        {
            Services = new Dictionary<string, FranzGrpcClientServiceConfig>(StringComparer.OrdinalIgnoreCase)
            {
                ["Orders"] = new() { BaseAddress = "http://localhost" }
            }
        });
        var factory = new FranzGrpcClientFactory(options);

        using var first = factory.CreateChannel("Orders");
        using var second = factory.CreateChannel("Orders");

        // Characterizes current ownership behaviour: the factory does not pool/reuse channels.
        Assert.NotSame(first, second);
    }
}
