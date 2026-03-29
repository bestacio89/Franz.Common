using System;
using System.Threading;
using System.Threading.Tasks;
using Franz.Common.Messaging.RabbitMQ.Connections;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Franz.Common.Http.Messaging.Health;

public class RabbitMQConnectionHealthCheck : IHealthCheck
{
    private readonly IConnectionProvider _connectionProvider;

    public RabbitMQConnectionHealthCheck(IConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connection = await _connectionProvider.GetConnectionAsync(cancellationToken);
            return connection.IsOpen
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("RabbitMQ connection is not open.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception during RabbitMQ health check.", ex);
        }
    }
}
