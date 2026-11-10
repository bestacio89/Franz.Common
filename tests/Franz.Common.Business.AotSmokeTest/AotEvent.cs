using Franz.Common.Mediator.Messages;

namespace Franz.Common.Business.AotSmokeTest;

public sealed record AotEvent(DateTimeOffset OccurredOn) : IEvent;