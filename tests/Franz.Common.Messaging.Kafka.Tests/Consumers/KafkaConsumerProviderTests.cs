#nullable enable
using Confluent.Kafka;
using FluentAssertions;
using Franz.Common.Messaging.Kafka.Configuration;
using Franz.Common.Messaging.Kafka.Tests.Fixtures;
using Franz.Common.Messaging.KafKa.Consumers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.Kafka.Tests.Consumers
{
  [Collection("KafkaConsumer")]
  public sealed class KafkaConsumerProviderIntegrationTests
  {
    private readonly KafkaContainerFixture _fixture;

    public KafkaConsumerProviderIntegrationTests(KafkaContainerFixture fixture)
    {
      _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
    }

    private KafkaConsumerProvider CreateProvider(string? groupId = null)
    {
      var cleanedAddress = _fixture.BootstrapServers
          .Replace("plaintext://", "", StringComparison.OrdinalIgnoreCase)
          .TrimEnd('/');

      var options = Options.Create(new KafkaMessagingOptions
      {
        GroupId = $"test-group-{Guid.NewGuid():N}",
        BootstrapServers = cleanedAddress,
        Consumer = new KafkaConsumerOptions
        {
        }
      });

      return new KafkaConsumerProvider(options, NullLogger<KafkaConsumerProvider>.Instance);
    }

    [Fact]
    public void CreateConsumer_Should_Create_IndependentInstances()
    {
      var provider = CreateProvider();
      using var c1 = provider.CreateConsumer();
      using var c2 = provider.CreateConsumer();

      c1.Should().NotBeSameAs(c2);
    }

    [Fact]
    public void CreateConsumer_Should_ReturnValidHandle()
    {
      var provider = CreateProvider();
      using var consumer = provider.CreateConsumer();

      consumer.Handle.Should().NotBeNull();
      Library.Version.Should().NotBe(0);
    }

    [Fact]
    public async Task Consumer_Should_SubscribeAndConsumeMessage()
    {
      var topic = $"integration-test-topic-{Guid.NewGuid():N}";
      var provider = CreateProvider();

      // Use AdminClient to create the topic in Kafka
      using var admin = new AdminClientBuilder(new Confluent.Kafka.AdminClientConfig
      {
        BootstrapServers = _fixture.BootstrapServers
      }).Build();

      await admin.CreateTopicsAsync(new[]
      {
                new Confluent.Kafka.Admin.TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            });

      using var consumer = provider.CreateConsumer();
      consumer.Subscribe(topic);

      // Produce a test message
      using var producer = new ProducerBuilder<Null, string>(
          new ProducerConfig { BootstrapServers = _fixture.BootstrapServers }).Build();

      var testMessage = "hello-kafka";
      await producer.ProduceAsync(topic, new Message<Null, string> { Value = testMessage });

      var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
      var consumed = consumer.Consume(cts.Token);
      consumed.Message.Value.Should().Be(testMessage);

      consumer.Unsubscribe();
    }

    [Fact]
    public void CreateConsumer_ShouldThrow_WhenBootstrapServersMissing()
    {
      var options = Options.Create(new KafkaMessagingOptions { BootstrapServers = string.Empty, GroupId = "welps" });
      var provider = new KafkaConsumerProvider(options, NullLogger<KafkaConsumerProvider>.Instance);

      Action act = () => provider.CreateConsumer();

      act.Should().Throw<ArgumentException>()
          .WithMessage("*BootstrapServers must be configured*");
    }
  }
}