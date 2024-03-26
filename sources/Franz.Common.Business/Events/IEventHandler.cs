namespace Franz.Common.Business.Events;

public interface IEventHandler<in TEvent> : INotificationHandler<TEvent>
  where TEvent : IEvent
{
}
