using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcLegacyContractRemovalTests
{
    [Fact]
    public void CoreAssembly_DoesNotExposeManualServiceBaseOrChannelFactory()
    {
        var assembly = typeof(GrpcCallContext).Assembly;

        Assert.Null(assembly.GetType("Franz.Common.Grpc.Server.FranzServiceBase"));
        Assert.Null(assembly.GetType("Franz.Common.Grpc.Client.IFranzGrpcClientFactory"));
        Assert.Null(assembly.GetType("Franz.Common.Grpc.Client.FranzGrpcClientFactory"));
    }

    [Fact]
    public void CoreAssembly_ReferencesGrpcApiWithoutClientOrAspNetCoreTransport()
    {
        var references = typeof(GrpcCallContext).Assembly.GetReferencedAssemblies();
        var referenceNames = references.Select(reference => reference.Name).ToArray();

        Assert.Contains("Grpc.Core.Api", referenceNames);
        Assert.DoesNotContain("Grpc.Net.Client", referenceNames);
        Assert.DoesNotContain(referenceNames, name =>
            name is not null && name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal));
    }
}
