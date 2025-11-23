using Grpc.Net.Client;

namespace Franz.Common.Grpc.Abstractions;

public interface IFranzGrpcClientFactory
{
  /// <summary>
  /// Creates a configured gRPC channel for the given service name.
  /// </summary>
  GrpcChannel CreateChannel(string serviceName);

  /// <summary>
  /// Creates a typed gRPC client using the configured behaviors and options.
  /// </summary>
  TClient CreateClient<TClient>(string serviceName)
      where TClient : class;
}
