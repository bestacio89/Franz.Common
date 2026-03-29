using FluentAssertions;
using Franz.Common.Messaging.RabbitMQ.Tests.Infrastructure;
using System.Reflection;
using Xunit;
using Xunit;
using Franz.Common.Messaging;
using Franz.Common.Reflection;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Naming;

[Collection(nameof(RabbitMqTestCollection))]
public class NamingTests
{
    [Fact]
    public void GetQueueName_ShouldReturnLowerCasedDashedName()
    {
        // Arrange
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        // Act
        var queueName = QueueNamer.GetQueueName(assembly);

        // Assert
        queueName.Should().Be("franz-common-in");
    }

    [Fact]
    public void GetDeadLetterQueueName_ShouldAppendDlq()
    {
        // Arrange
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        // Act
        var queueName = QueueNamer.GetDeadLetterQueueName(assembly);

        // Assert
        queueName.Should().Be("franz-common-in-dlq");
    }

    [Fact] 
    public void GetEventExchangeName_Should_Return_Correct_Name()
    {
        // Arrange
        var assembly = new AssemblyWrapper(Assembly.GetExecutingAssembly());

        // Act
        var exchangeName = ExchangeNamer.GetEventExchangeName(assembly);

        // Assert
        exchangeName.Should().Be("franz-common-event");
    }
}
