#nullable enable
using Franz.Common.Hosting;

namespace Franz.Common.Messaging.Kafka;

/// <summary>
/// Specialized Kafka hosting initializer.
/// Marked as sealed to enable JIT devirtualization optimizations in .NET 10.
/// </summary>
public sealed class MessagingHostingInitializer(IMessagingInitializer? messagingInitializer = null) : IHostingInitializer
{
    // Implementation of IHostingInitializer
    public int Order => 2;

    public void Initialize()
    {
        // Executes the captured messaging initialization logic
        messagingInitializer?.InitializeAsync();
    }
}