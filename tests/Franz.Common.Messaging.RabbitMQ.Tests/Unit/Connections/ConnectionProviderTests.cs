using Franz.Common.Messaging.RabbitMQ.Connections;
using Moq;
using RabbitMQ.Client;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Franz.Common.Messaging.RabbitMQ.Tests.Unit.Connections;

public class ConnectionProviderTests
{
    [Fact]
    public async Task GetConnectionAsync_ShouldReturnSameConnection_WhenCalledInParallel()
    {
        // Arrange
        var mockFactory = new Mock<IConnection>();
        mockFactory.Setup(c => c.IsOpen).Returns(true);
        
        var mockFactoryProvider = new Mock<IConnectionFactoryProvider>();
        var mockConnectionFactory = new Mock<IConnectionFactory>();
        
        mockConnectionFactory
            .Setup(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockFactory.Object);
            
        mockFactoryProvider.Setup(p => p.Current).Returns(mockConnectionFactory.Object);
        
        var provider = new ConnectionProvider(mockFactoryProvider.Object);
        var connections = new ConcurrentBag<IConnection>();

        // Act
        var tasks = Enumerable.Range(0, 50).Select(_ => Task.Run(async () => 
        {
            var conn = await provider.GetConnectionAsync();
            connections.Add(conn);
        }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.Single(connections.Distinct());
        mockConnectionFactory.Verify(f => f.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
