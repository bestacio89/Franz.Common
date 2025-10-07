using Franz.Common.Errors;
using Franz.Common.Messaging.Contexting;
using Franz.Common.Messaging.Hosting.Executing;
using Franz.Common.Messaging.Hosting.Properties;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Franz.Common.Messaging.Hosting.RabbitMQ.HostedServices;

public class MessagingHostedService : IHostedService
{
    private readonly IListener listener;
    private readonly IServiceProvider serviceProvider;

    public MessagingHostedService(IListener listener, IServiceProvider serviceProvider)
    {
        this.listener = listener;
        this.serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
        listener.Received += Listener_Received;
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

        await Task.Run(() => listener.Listen(), cancellationToken);
    }

    internal void Listener_Received(object sender, MessageEventArgs messageEventArgs)
    {
        var message = messageEventArgs.Message;

        using var serviceProviderScope = serviceProvider.CreateScope();
        var messageContextAccessor = serviceProviderScope.ServiceProvider.GetRequiredService<MessageContextAccessor>();
        messageContextAccessor.Set(new MessageContext(message));

        var messagingStrategyExecuters = serviceProviderScope.ServiceProvider.GetServices<IMessagingStrategyExecuter>();

        var messagingStrategyExecuter = messagingStrategyExecuters
          .FirstOrDefault(x => x.CanExecuteAsync(message).Result);

        if (messagingStrategyExecuter == null)
            throw new TechnicalException(Resources.StrategyExecuterNotFoundException);

        messagingStrategyExecuter.ExecuteAsync(message).Wait();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() => listener.StopListen(), cancellationToken);
    }
}
