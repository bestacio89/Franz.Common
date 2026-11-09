using Franz.Common.Grpc.Configuration;
using Franz.Common.Grpc.DependencyInjection;
using Franz.Common.Grpc.AspNetCore.Configuration;
using Franz.Common.Grpc.AspNetCore.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Grpc.Tests.Unit;

public sealed class GrpcConfigurationOptionsTests
{
    [Fact]
    public void AddFranzGrpcClient_UsesDocumentedDefaultTimeout()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcClient(new ConfigurationBuilder().Build());

        using var provider = services.BuildServiceProvider();

        Assert.Equal(
            TimeSpan.FromSeconds(15),
            provider.GetRequiredService<IOptions<FranzGrpcClientOptions>>().Value.DefaultClientTimeout);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    [InlineData("366.00:00:00")]
    public void AddFranzGrpcClient_RejectsInvalidDefaultTimeout(string timeout)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Franz:Grpc:Client:DefaultClientTimeout"] = timeout
            })
            .Build();
        var services = new ServiceCollection();
        services.AddFranzGrpcClient(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<FranzGrpcClientOptions>>().Value);
    }

    [Fact]
    public void AddFranzGrpcAspNetCore_RejectsEmptyAuthenticatedUserClaimType()
    {
        var services = new ServiceCollection();
        services.AddFranzGrpcAspNetCore(options => options.UserIdClaimType = " ");

        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<FranzGrpcAspNetCoreOptions>>().Value);
    }

    [Fact]
    public void AddFranzGrpcServer_BindsTenantRequirement()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Franz:Grpc:RequireTenant"] = "true"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddFranzGrpcServer(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.True(provider.GetRequiredService<IOptions<FranzGrpcOptions>>().Value.RequireTenant);
    }
}
