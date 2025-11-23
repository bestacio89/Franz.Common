using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

public sealed class NoOpValidationEngine : IFranzValidationEngine
{
  public Task ValidateAsync<T>(T instance, string? ruleSet = null, CancellationToken cancellationToken = default)
      where T : class
  {
    // No validation performed
    return Task.CompletedTask;
  }
}
