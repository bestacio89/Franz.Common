using Franz.Common.MultiTenancy.TenantResolution;

namespace Franz.Common.MultiTenancy;

/// <summary>Runs all strategies and rejects contradictory successful tenant identities.</summary>
public sealed class DefaultTenantResolutionPipeline : ITenantResolutionPipeline
{
    private readonly IReadOnlyList<ITenantResolver> _resolvers;

    public DefaultTenantResolutionPipeline(IEnumerable<ITenantResolver> resolvers)
    {
        ArgumentNullException.ThrowIfNull(resolvers);
        _resolvers = resolvers.OrderBy(resolver => resolver.Order).ToArray();
    }

    public Task<TenantResolutionResult?> ResolveAsync(object? context = null)
        => ResolveAsync(context, CancellationToken.None);

    public async Task<TenantResolutionResult?> ResolveAsync(
        object? context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        TenantResolutionResult? resolved = null;
        foreach (var resolver in _resolvers)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var candidate = await resolver.ResolveTenantAsync(context, cancellationToken).ConfigureAwait(false);
            if (candidate?.Tenant is null || !candidate.Succeeded) continue;
            if (resolved is null) { resolved = candidate; continue; }
            if (resolved.Tenant.Id != candidate.Tenant.Id)
                return TenantResolutionResult.FailedResult(candidate.Source, $"Conflicting tenant identities were supplied by {resolved.Source} and {candidate.Source}.");
        }
        return resolved;
    }
}
