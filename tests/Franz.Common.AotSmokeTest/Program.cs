using Franz.Common.Mapping.Abstractions;
using Franz.Common.Mediator.Dispatchers;
using Franz.Common.Mediator.Extensions;
using Franz.Common.Mediator.Handlers;
using Franz.Common.Mediator.Messages;
using Franz.Common.Mediator.Pipelines.Processors;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace Franz.Common.AotSmokeTest;

public static class Program
{
    public static async Task<int> Main()
    {
        var source = new AotSource("Franz", 10);
        var destination = new AotMapper().Map(source);
        if (destination.Name != source.Name || destination.Version != source.Version)
            return 1;

        var services = new ServiceCollection();
        services.AddFranzMediator(new[] { typeof(AotCommandHandler).Assembly });
        services.AddScoped<IPreProcessor<AotCommand>, AotCommandPreProcessor>();
        services.AddScoped<IPostProcessor<AotCommand, string>, AotCommandPostProcessor>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();

        var commandResult = await dispatcher.SendAsync<string>(new AotCommand("generated-command"));
        if (commandResult != "generated-command")
            return 2;

        var queryResult = await dispatcher.SendAsync<int>(new AotQuery(21));
        if (queryResult != 42)
            return 3;

        await dispatcher.SendAsync(new AotVoidCommand());
        if (!scope.ServiceProvider.GetRequiredService<ICommandHandler<AotVoidCommand, Unit>>().Equals(null))
            return 4;

        var notification = new AotNotification();
        await dispatcher.PublishNotificationAsync(notification);
        if (!scope.ServiceProvider.GetRequiredService<INotificationHandler<AotNotification>>().Equals(null))
            return 5;

        var @event = new AotEvent(DateTimeOffset.UtcNow);
        await dispatcher.PublishEventAsync(@event);
        if (!scope.ServiceProvider.GetRequiredService<IEventHandler<AotEvent>>().Equals(null))
            return 6;

        var stream = (FranzDispatcher)dispatcher;
        var values = new List<int>();
        await foreach (var value in stream.Stream<AotStreamQuery, int>(new AotStreamQuery(3)))
            values.Add(value);
        if (!values.SequenceEqual(new[] { 1, 2, 3 }))
            return 7;

        return 0;
    }
}

public sealed record AotSource(string Name, int Version);

public sealed class AotDestination
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
}

public partial class AotMapper : IMapper<AotSource, AotDestination>
{
    public partial AotDestination Map(AotSource source);
}

public sealed record AotCommand(string Value) : ICommand<string>;
public sealed class AotCommandHandler : ICommandHandler<AotCommand, string>
{
    public Task<string> Handle(AotCommand command, CancellationToken cancellationToken)
        => Task.FromResult(command.Value);
}

public sealed class AotCommandPreProcessor : IPreProcessor<AotCommand>
{
    public Task ProcessAsync(AotCommand request, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class AotCommandPostProcessor : IPostProcessor<AotCommand, string>
{
    public Task ProcessAsync(AotCommand request, string response, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed record AotQuery(int Id) : IQuery<int>;
public sealed class AotQueryHandler : IQueryHandler<AotQuery, int>
{
    public Task<int> Handle(AotQuery query, CancellationToken cancellationToken)
        => Task.FromResult(query.Id * 2);
}

public sealed record AotVoidCommand : ICommand;
public sealed class AotVoidCommandHandler : ICommandHandler<AotVoidCommand>
{
    public Task Handle(AotVoidCommand command, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public sealed record AotNotification : INotification;
public sealed class AotNotificationHandler : INotificationHandler<AotNotification>
{
    public Task Handle(AotNotification notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed record AotEvent(DateTimeOffset OccurredOn) : IEvent;
public sealed class AotEventHandler : IEventHandler<AotEvent>
{
    public Task HandleAsync(AotEvent @event, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed record AotStreamQuery(int Count) : IStreamQuery<int>;
public sealed class AotStreamQueryHandler : IStreamQueryHandler<AotStreamQuery, int>
{
    public async IAsyncEnumerable<int> Handle(
        AotStreamQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (var index = 1; index <= query.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield return index;
        }
    }
}
