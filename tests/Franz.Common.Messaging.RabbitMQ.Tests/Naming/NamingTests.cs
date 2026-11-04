using FluentAssertions;
using Franz.Common.Messaging.RabbitMQ;
using Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;
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

    [Fact]
    public void SharedCompatibilityNamers_ShouldRemainAvailableDuringTheCurrentMajorVersion()
    {
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        global::Franz.Common.Messaging.QueueNamer.GetQueueName(assembly)
            .Should().Be(RabbitMQQueueNamer.GetQueueName(assembly));
        global::Franz.Common.Messaging.ExchangeNamer.GetEventExchangeName(assembly)
            .Should().Be(RabbitMQExchangeNamer.GetEventExchangeName(assembly));
    }
}
