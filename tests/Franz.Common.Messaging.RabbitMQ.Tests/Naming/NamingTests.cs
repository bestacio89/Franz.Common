using FluentAssertions;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;
using Franz.Common.Messaging.Messages;
using Franz.Common.Reflection;
using System.Reflection;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Naming;

[Collection(nameof(RabbitMqTestCollection))]
public class NamingTests
{
    [Fact]
    public void GetQueueName_ShouldReturnLowerCasedDashedName()
    {
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        RabbitMQQueueNamer.GetQueueName(assembly)
            .Should().Be("franz-common-in");
    }

    [Fact]
    public void GetDeadLetterQueueName_ShouldAppendDlq()
    {
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        RabbitMQQueueNamer.GetDeadLetterQueueName(assembly)
            .Should().Be("franz-common-in-dlq");
    }

    [Fact]
    public void GetEventExchangeName_ShouldReturnCorrectName()
    {
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        RabbitMQExchangeNamer.GetEventExchangeName(assembly)
            .Should().Be("franz-common-event");
    }

    [Theory]
    [InlineData(MessageKind.Command, "franz-commands-in")]
    [InlineData(MessageKind.Query, "franz-queries-in")]
    [InlineData(MessageKind.Fault, "franz-faults-in")]
    [InlineData(MessageKind.IntegrationEvent, "franz-events-in")]
    public void GetQueueName_ShouldMapMessageKindsToRabbitMqDestinations(
        MessageKind kind,
        string expectedQueue)
    {
        RabbitMQQueueNamer.GetQueueName(kind).Should().Be(expectedQueue);
    }

    [Fact]
    public void GetDeadLetterExchangeName_ShouldAppendDlq()
    {
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        RabbitMQExchangeNamer.GetDeadLetterExchangeName(assembly)
            .Should().Be("franz-common-dlq");
    }

    [Fact]
    public void GetReplayExchangeName_ShouldAppendReplay()
    {
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        RabbitMQExchangeNamer.GetReplayExchangeName(assembly)
            .Should().Be("franz-common-replay");
    }

}
