using System;
using System.Net.Http;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using Franz.Common.Grpc.Configuration;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Client;

public sealed class FranzGrpcClientFactory : IFranzGrpcClientFactory
{
  private readonly FranzGrpcClientOptions _options;

  public FranzGrpcClientFactory(IOptions<FranzGrpcClientOptions> options)
  {
    _options = options.Value ?? throw new ArgumentNullException(nameof(options));
  }

  public GrpcChannel CreateChannel(string serviceName)
  {
    if (!_options.Services.TryGetValue(serviceName, out var serviceConfig))
      throw new InvalidOperationException($"Unknown gRPC service: {serviceName}");

    var httpHandler = new HttpClientHandler
    {
      // Future: configure TLS, certs, proxies etc.
    };

    return GrpcChannel.ForAddress(serviceConfig.BaseAddress, new GrpcChannelOptions
    {
      HttpHandler = httpHandler
    });
  }

  public TClient CreateClient<TClient>(string serviceName)
      where TClient : class
  {
    var channel = CreateChannel(serviceName);

    // Construct using standard .NET gRPC factory pattern
    return Activator.CreateInstance(typeof(TClient), channel) as TClient
           ?? throw new InvalidOperationException(
               $"Could not construct gRPC client type {typeof(TClient).Name}.");
  }
}
