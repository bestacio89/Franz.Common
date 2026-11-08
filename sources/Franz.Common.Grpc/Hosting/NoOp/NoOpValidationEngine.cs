using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Grpc.Abstractions;

namespace Franz.Common.Grpc.Hosting.NoOp;

/// <summary>Validation implementation used when no validation engine is configured.</summary>
public sealed class NoOpValidationEngine : IFranzValidationEngine
{
    public Task ValidateAsync<T>(
        T instance,
        string? ruleSet = null,
        CancellationToken cancellationToken = default)
        where T : class
        => Task.CompletedTask;
}
