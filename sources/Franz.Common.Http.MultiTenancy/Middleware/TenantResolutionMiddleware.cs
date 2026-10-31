using Franz.Common.Http.MultiTenancy.Accessors;
using Franz.Common.Http.MultiTenancy.Configuration;
using Franz.Common.MultiTenancy;
using Franz.Common.MultiTenancy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Franz.Common.Http.MultiTenancy.Middleware;

/// <summary>Establishes a validated tenant context before application middleware and handlers execute.</summary>
public sealed class TenantResolutionMiddleware(
    RequestDelegate next,
    ITenantResolutionPipeline pipeline,
    ITenantExecutionContextAccessor executionContextAccessor,
    IOptions<TenantResolutionOptions> options,
    IServiceProvider serviceProvider)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var result = await pipeline.ResolveAsync(context).ConfigureAwait(false);
        var tenant = result?.Tenant;

        if (tenant is null || !result!.Succeeded)
        {
            if (options.Value.RequireTenant)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            await next(context).ConfigureAwait(false);
            return;
        }

        if (tenant.Status != TenantStatus.Active)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        var validator = serviceProvider.GetService<ITenantValidator>();
        if (validator is not null && !await validator.IsValidAsync(tenant.Id, context.RequestAborted).ConfigureAwait(false))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        Guid? domainId = null;
        var host = context.Request.Host.Host;
        if (!string.IsNullOrWhiteSpace(host))
        {
            var normalizedHost = TenantDomain.NormalizeHost(host);
            var domain = tenant.Domains.SingleOrDefault(candidate =>
                candidate.IsVerified && candidate.Host == normalizedHost &&
                (candidate.TenantId == Guid.Empty || candidate.TenantId == tenant.Id));
            if (domain is not null) domainId = domain.Id;
        }

        var executionContext = new TenantExecutionContext(tenant, domainId, result.Source, DateTimeOffset.UtcNow);
        executionContextAccessor.Initialize(executionContext);
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            executionContextAccessor.Clear();
        }
    }
}
