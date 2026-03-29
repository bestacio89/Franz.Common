using Xunit;
using Franz.Common.Messaging.Hosting.Kafka.Tests.Fixtures;

[CollectionDefinition("KafkaHosting")]
public class KafkaHostingCollection :
  ICollectionFixture<KafkaHostingFixture>
{
}