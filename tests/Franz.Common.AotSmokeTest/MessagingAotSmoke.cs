using Franz.Common.Messaging;
using Franz.Common.Messaging.Adapters;
using Franz.Common.Messaging.Manifest;
using Franz.Common.Messaging.Extensions;
using Franz.Common.Messaging.Factories;
using Franz.Common.Messaging.Messages;
using Franz.Common.Messaging.Serialization;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Franz.Common.AotSmokeTest;

public static class MessagingAotSmoke
{
    public static Task<int> RunAsync()
    {
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            try
            {
                _ = new JsonMessageSerializer();
                return Task.FromResult(24);
            }
            catch (InvalidOperationException)
            {
            }

            try
            {
                _ = new IntegrationEventMessageBuilderStrategy();
                return Task.FromResult(25);
            }
            catch (InvalidOperationException)
            {
            }
        }

        var services = new ServiceCollection();
        services
            .AddMessagingSerialization(AotMessagingJsonContext.Default)
            .AddMessagingFactories();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        using var scope = provider.CreateScope();
        var scopedServices = scope.ServiceProvider;

        MessagingManifestRegistry.Register(
            typeof(AotCommand).Assembly,
            new MessagingManifest(
                destinations: null,
                subscriptions: new[] { typeof(AotEvent) },
                knownTypes: new[] { typeof(AotCommand), typeof(AotEvent) }));

        var integrationEvent = new AotIntegrationEvent(
            Guid.Parse("01936f4a-7c2b-7a31-9f23-8f8d6f9b1c42"),
            "source-generated-messaging");

        var factory = scopedServices.GetRequiredService<IMessageFactory>();
        var message = factory.Build(integrationEvent);
        if (message.Kind != MessageKind.IntegrationEvent)
            return Task.FromResult(20);

        if (!message.Headers.TryGetValue(MessagingConstants.ClassName, out var classNames) ||
            classNames.Length != 1 ||
            string.IsNullOrWhiteSpace(classNames[0]))
            return Task.FromResult(21);

        var serializer = scopedServices.GetRequiredService<IMessageSerializer>();
        var roundTripped = serializer.Deserialize<AotIntegrationEvent>(message.Body!);
        if (roundTripped != integrationEvent)
            return Task.FromResult(22);

        var command = new AotCommand("source-generated-command");
        var commandMessage = factory.Build(command);
        if (commandMessage.Kind != MessageKind.Command ||
            serializer.Deserialize<AotCommand>(commandMessage.Body!) != command)
            return Task.FromResult(26);

        var commandEnvelope = command.ToMessage(serializer);
        var inboundCommand = commandEnvelope.ToCommand(serializer) as AotCommand;
        if (inboundCommand?.Value != command.Value ||
            inboundCommand.CorrelationId != commandEnvelope.CorrelationId)
            return Task.FromResult(29);

        var @event = new AotEvent(DateTimeOffset.UnixEpoch);
        var eventEnvelope = @event.ToMessage(serializer);
        var inboundEvent = eventEnvelope.ToEvent(serializer) as AotEvent;
        if (inboundEvent?.OccurredOn != @event.OccurredOn ||
            inboundEvent.CorrelationId != eventEnvelope.CorrelationId)
            return Task.FromResult(30);

        var query = new AotQuery(42);
        var queryMessage = factory.Build(query);
        if (queryMessage.Kind != MessageKind.Query ||
            serializer.Deserialize<AotQuery>(queryMessage.Body!) != query)
            return Task.FromResult(27);

        var faultMessage = factory.Build(new InvalidOperationException("source-generated-fault"));
        if (faultMessage.Kind != MessageKind.Fault ||
            serializer.Deserialize<ExecutionFault>(faultMessage.Body!) is null)
            return Task.FromResult(28);

        var serialized = serializer.Serialize(integrationEvent);
        var secondRoundTrip = serializer.Deserialize<AotIntegrationEvent>(serialized);
        return Task.FromResult(secondRoundTrip == integrationEvent ? 0 : 23);
    }
}
