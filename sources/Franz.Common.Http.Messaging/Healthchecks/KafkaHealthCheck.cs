using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

public class KafkaHealthCheck : IHealthCheck
{
  private readonly ConsumerConfig _consumerConfig;

  public KafkaHealthCheck(ConsumerConfig consumerConfig)
  {
    _consumerConfig = consumerConfig ?? throw new ArgumentNullException(nameof(consumerConfig));
  }

  public async Task<HealthCheckResult> CheckHealthAsync(
      HealthCheckContext context,
      CancellationToken cancellationToken = default)
  {
    try
    {
      return await Task.Run(() =>
      {
        using var adminClient = new AdminClientBuilder(_consumerConfig).Build();
        var timeout = TimeSpan.FromSeconds(10);
        var metadata = adminClient.GetMetadata(timeout);

        if (metadata.Topics.Count > 0)
          return HealthCheckResult.Healthy("Successfully connected to Kafka.");
        else
          return HealthCheckResult.Unhealthy("No topics found in Kafka metadata.");
      }, cancellationToken);
    }
    catch (Exception ex)
    {
      return HealthCheckResult.Unhealthy($"Failed to connect to Kafka: {ex.Message}");
    }
  }

}
