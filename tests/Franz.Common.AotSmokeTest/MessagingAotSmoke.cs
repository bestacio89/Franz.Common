using Franz.Common.Messaging;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;

namespace Franz.Common.AotSmokeTest;

public static class MessagingAotSmoke
{
    public static Task<int> RunAsync()
    {
        var services = new ServiceCollection();
        services
            .AddMessagingSerialization(AotMessagingJsonContext.Default)
            .AddMessagingFactories();

        using var provider = services.BuildServiceProvider();
        var integrationEvent = new AotIntegrationEvent(
            Guid.Parse("01936f4a-7c2b-7a31-9f23-8f8d6f9b1c42"),
            "source-generated-messaging");

        var message = provider.GetRequiredService<IMessageFactory>().Build(integrationEvent);
        if (message.Kind != MessageKind.IntegrationEvent)
            return Task.FromResult(20);

        if (!message.Headers.TryGetValue(MessagingConstants.ClassName, out var classNames) ||
            classNames.Length != 1 ||
            string.IsNullOrWhiteSpace(classNames[0]))
            return Task.FromResult(21);

        var serializer = provider.GetRequiredService<IMessageSerializer>();
        var roundTripped = serializer.Deserialize<AotIntegrationEvent>(message.Body!);
        if (roundTripped != integrationEvent)
            return Task.FromResult(22);

        var serialized = serializer.Serialize(integrationEvent);
        var secondRoundTrip = serializer.Deserialize<AotIntegrationEvent>(serialized);
        return Task.FromResult(secondRoundTrip == integrationEvent ? 0 : 23);
    }
}
