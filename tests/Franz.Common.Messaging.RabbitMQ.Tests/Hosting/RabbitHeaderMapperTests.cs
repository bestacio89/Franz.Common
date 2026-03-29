using FluentAssertions;
using Franz.Common.Messaging.RabbitMQ.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Hosting;

public class RabbitHeaderMapperTests
{
    [Fact]
    public void ExtractHeaders_ShouldMapRabbitMQHeadersToMessageHeaders()
    {
        // Arrange
        var properties = new BasicProperties();
        properties.Headers = new Dictionary<string, object>
        {
            { "TenantId", "tenant-1" },
            { "ByteArrayHeader", Encoding.UTF8.GetBytes("byte-value") },
            { "ListHeader", new List<object> { "val1", Encoding.UTF8.GetBytes("val2") } },
            { "x-ignore-me", "ignored" }
        };

        var eventArgs = new BasicDeliverEventArgs("tag", 1, false, "exchange", "routingKey", properties, default);

        // Act
        var headers = RabbitHeaderMapper.ExtractHeaders(eventArgs);

        // Assert
        headers.Should().ContainKey("TenantId")
            .WhoseValue.Should().BeEquivalentTo(new[] { "tenant-1" });

        headers.Should().ContainKey("ByteArrayHeader")
            .WhoseValue.Should().BeEquivalentTo(new[] { "byte-value" });

        headers.Should().ContainKey("ListHeader")
            .WhoseValue.Should().BeEquivalentTo(new[] { "val1", "val2" });

        headers.Should().NotContainKey("x-ignore-me");
    }

    [Fact]
    public void ExtractHeaders_ShouldHandleNullHeaders()
    {
        // Arrange
        var properties = new BasicProperties();
        var eventArgs = new BasicDeliverEventArgs("tag", 1, false, "exchange", "routingKey", properties, default);

        // Act
        var headers = RabbitHeaderMapper.ExtractHeaders(eventArgs);

        // Assert
        headers.Should().BeEmpty();
    }
}
